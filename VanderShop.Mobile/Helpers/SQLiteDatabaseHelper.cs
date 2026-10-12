using SQLite;
using VanderShop.Models;

namespace VanderShop.Helpers;

public class SQLiteDatabaseHelper
{
    private readonly SQLiteAsyncConnection _conexao;
    private readonly SemaphoreSlim _travaInicializacao = new(1, 1);
    private bool _inicializado;

    public SQLiteDatabaseHelper(string caminho)
    {
        _conexao = new SQLiteAsyncConnection(caminho);
    }

    public async Task InicializarAsync()
    {
        if (_inicializado) return;
        await _travaInicializacao.WaitAsync();
        try
        {
            if (_inicializado) return;
            await _conexao.CreateTableAsync<Usuarios>();
            await _conexao.CreateTableAsync<Categorias>();
            await _conexao.CreateTableAsync<Produtos>();
            await _conexao.CreateTableAsync<Pedidos>();
            await _conexao.CreateTableAsync<ItensPedido>();
            _inicializado = true;
        }
        finally { _travaInicializacao.Release(); }
    }

    public async Task LimparSessaoAsync()
    {
        await InicializarAsync();
        await _conexao.RunInTransactionAsync(banco =>
        {
            banco.DeleteAll<ItensPedido>();
            banco.DeleteAll<Pedidos>();
            banco.DeleteAll<Usuarios>();
        });
    }

    public async Task SalvarSessaoAsync(Usuarios usuario, PedidosResposta dados)
    {
        await InicializarAsync();
        await _conexao.RunInTransactionAsync(banco =>
        {
            banco.DeleteAll<ItensPedido>();
            banco.DeleteAll<Pedidos>();
            banco.DeleteAll<Usuarios>();
            banco.Insert(usuario);
            banco.InsertAll(dados.Pedidos);
            banco.InsertAll(dados.ItensPedido);
        });
    }

    public async Task SalvarCatalogoAsync(CatalogoResposta dados)
    {
        await InicializarAsync();
        await _conexao.RunInTransactionAsync(banco =>
        {
            banco.DeleteAll<Produtos>();
            banco.DeleteAll<Categorias>();
            banco.InsertAll(dados.Categorias);
            banco.InsertAll(dados.Produtos);
        });
    }

    public async Task SalvarPedidoRecebidoAsync(PedidoResposta dados)
    {
        await InicializarAsync();
        await _conexao.RunInTransactionAsync(banco =>
        {
            banco.InsertOrReplace(dados.Pedido);
            banco.Execute("DELETE FROM ItensPedido WHERE PedidoId = ?", dados.Pedido.Id);
            banco.InsertAll(dados.ItensPedido);

            if (dados.Pedido.FormaRecebimento == OpcoesCompra.Entrega)
            {
                var p = dados.Pedido;
                banco.Execute(
                    "UPDATE Usuarios SET CEP=?, Rua=?, Bairro=?, Numero=?, Complemento=? WHERE Id=?",
                    p.CEP, p.Rua, p.Bairro, p.Numero, p.Complemento, p.UsuarioId);
            }
        });
    }

    public async Task<Usuarios?> ObterUsuarioAsync(int id)
    {
        await InicializarAsync();
        return await _conexao.FindAsync<Usuarios>(id);
    }

    public async Task<List<Categorias>> ObterCategoriasAsync()
    {
        await InicializarAsync();
        return await _conexao.Table<Categorias>()
            .OrderBy(c => c.Categoria).ToListAsync();
    }

    public async Task<List<Produtos>> ObterProdutosAsync()
    {
        await InicializarAsync();
        var produtos = await _conexao.Table<Produtos>()
            .Where(p => p.Ativo).OrderBy(p => p.Produto).ToListAsync();
        var categorias = (await ObterCategoriasAsync()).ToDictionary(c => c.Id);

        foreach (var produto in produtos)
        {
            produto.aCategoria = categorias.TryGetValue(produto.CategoriaId, out var categoria)
                ? categoria : new Categorias { Categoria = "Sem categoria" };
        }
        return produtos;
    }

    public async Task<List<Pedidos>> ObterPedidosAsync(int usuarioId)
    {
        await InicializarAsync();
        return await _conexao.Table<Pedidos>()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.DataHora).ToListAsync();
    }

    public async Task<(Pedidos Pedido, List<ItensPedido> Itens)>
        ObterDetalhesAsync(int pedidoId, int usuarioId)
    {
        await InicializarAsync();
        var pedido = await _conexao.Table<Pedidos>()
            .Where(p => p.Id == pedidoId && p.UsuarioId == usuarioId)
            .FirstOrDefaultAsync();

        if (pedido == null)
            throw new InvalidOperationException("Pedido não encontrado para este usuário.");

        var itens = await _conexao.Table<ItensPedido>()
            .Where(i => i.PedidoId == pedidoId).OrderBy(i => i.Id).ToListAsync();
        return (pedido, itens);
    }
}