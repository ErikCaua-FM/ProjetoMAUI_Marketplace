using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Text.Json;
using VanderShop.Helpers;
using VanderShop.Models;
using VanderShop.Views;

namespace VanderShop;

public partial class App : Application
{
    private static readonly Lazy<SQLiteDatabaseHelper> _db =
        new(() => new SQLiteDatabaseHelper(Path.Combine(
            FileSystem.Current.AppDataDirectory, "vandershop_api_v1.db3")));

    private static readonly SemaphoreSlim _travaSessao = new(1, 1);
    private static bool _inicializado;
    private static string? _pedidoPendenteJson;

    public static SQLiteDatabaseHelper DbInstance => _db.Value;
    public static ApiService Api { get; } = new();
    public static Usuarios? UsuarioLogado { get; private set; }
    public static List<ItemCarrinho> Carrinho { get; } = new();
    public static bool CompraPendente => _pedidoPendenteJson is not null;
    public static string AvisoCache { get; private set; } = "";
    public static Pedidos? DadosCompraPendente => _pedidoPendenteJson is null
        ? null : JsonSerializer.Deserialize<Pedidos>(_pedidoPendenteJson);

    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        MainPage = new NavigationPage(new frmPedidos());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Width = 450;
        window.Height = 812;
        return window;
    }

    public static async Task InicializarAsync()
    {
        await _travaSessao.WaitAsync();
        try
        {
            if (_inicializado) return;
            await DbInstance.InicializarAsync();
            await LimparSessaoInternaAsync();
            Preferences.Default.Remove("usuario_demo_v2_logado");
            _inicializado = true;
        }
        finally { _travaSessao.Release(); }
    }

    public static async Task EntrarAsync(string email, string senha)
    {
        await InicializarAsync();
        await _travaSessao.WaitAsync();
        try
        {
            await LimparSessaoInternaAsync();
            var usuario = await Api.EntrarAsync(email, senha);
            var dados = await Api.ObterPedidosAsync();
            await DbInstance.SalvarSessaoAsync(usuario, dados);
            UsuarioLogado = usuario;
        }
        catch
        {
            await LimparSessaoInternaAsync();
            throw;
        }
        finally { _travaSessao.Release(); }
    }

    public static async Task SairAsync()
    {
        await _travaSessao.WaitAsync();
        try { await LimparSessaoInternaAsync(); }
        finally { _travaSessao.Release(); }
    }

    private static async Task LimparSessaoInternaAsync()
    {
        Api.LimparSessao();
        UsuarioLogado = null;
        Carrinho.Clear();
        _pedidoPendenteJson = null;
        AvisoCache = "";
        await DbInstance.LimparSessaoAsync();
    }

    public static async Task AtualizarPedidosAsync()
    {
        await _travaSessao.WaitAsync();
        try
        {
            if (UsuarioLogado is null) return;
            var dados = await Api.ObterPedidosAsync();
            var usuario = await Api.ObterUsuarioAsync();
            await DbInstance.SalvarSessaoAsync(usuario, dados);
            UsuarioLogado = usuario;
        }
        catch (ApiException erro) when (erro.Status == 401)
        {
            await LimparSessaoInternaAsync();
            throw;
        }
        finally { _travaSessao.Release(); }
    }

    public static async Task AtualizarCatalogoAsync()
    {
        var dados = await Api.ObterCatalogoAsync();
        await DbInstance.SalvarCatalogoAsync(dados);
        if (CompraPendente) return;

        foreach (var item in Carrinho)
        {
            var atual = dados.Produtos.FirstOrDefault(p => p.Id == item.oProduto.Id);
            if (atual is not null) item.oProduto = atual;
        }
    }

    public static async Task<int> SalvarPedidoAsync(
        Pedidos pedido, IReadOnlyCollection<ItemCarrinho> carrinho)
    {
        await _travaSessao.WaitAsync();
        try
        {
            if (UsuarioLogado is null)
                throw new ApiException(401, "Faça login para finalizar a compra.");

            if (_pedidoPendenteJson is null)
            {
                if (carrinho.Count == 0)
                    throw new InvalidOperationException("Adicione produtos ao carrinho.");

                _pedidoPendenteJson = JsonSerializer.Serialize(new
                {
                    pedido.ChaveLocal,
                    pedido.FormaPagamento,
                    pedido.FormaRecebimento,
                    pedido.CEP,
                    pedido.Rua,
                    pedido.Bairro,
                    pedido.Numero,
                    pedido.Complemento,
                    Itens = carrinho.Select(item => new
                    {
                        ProdutoId = item.oProduto.Id,
                        item.Quantidade,
                        PrecoUnitarioCentavos = item.oProduto.PrecoCentavos
                    }).ToList()
                });
            }

            PedidoResposta resposta;
            try { resposta = await Api.SalvarPedidoAsync(_pedidoPendenteJson); }
            catch (ApiException erro) when (erro.Status is 400 or 404 or 409 or 422)
            {
                _pedidoPendenteJson = null;
                throw;
            }

            AvisoCache = "";
            try { await DbInstance.SalvarPedidoRecebidoAsync(resposta); }
            catch
            {
                AvisoCache = "O pedido está salvo no MySQL, mas o cache local não " +
                    "pôde ser atualizado. Atualize a lista de pedidos.";
            }

            _pedidoPendenteJson = null;
            Carrinho.Clear();
            return resposta.Pedido.Id;
        }
        catch (ApiException erro) when (erro.Status == 401)
        {
            await LimparSessaoInternaAsync();
            throw;
        }
        finally { _travaSessao.Release(); }
    }
}