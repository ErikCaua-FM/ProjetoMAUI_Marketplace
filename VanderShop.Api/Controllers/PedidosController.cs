using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderShop.Api.Infrastructure;
using VanderShop.Api.Models;
using VanderShop.Api.Requests;

namespace VanderShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Cliente")]
[Route("api/pedidos")]
public class PedidosController(Banco banco) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = Regras.UsuarioId(User);

        await using var conexao = await banco.AbrirAsync();
        await using var transacao =
            await conexao.BeginTransactionAsync();

        var pedidos = (
            await conexao.QueryAsync<Pedidos>(
                """
                SELECT * FROM Pedidos
                WHERE UsuarioId = @usuarioId
                ORDER BY DataHora DESC, Id DESC
                """,
                new { usuarioId },
                transacao
            )
        ).ToList();

        var itens = (
            await conexao.QueryAsync<ItensPedido>(
                """
                SELECT i.*
                FROM ItensPedido i
                INNER JOIN Pedidos p ON p.Id = i.PedidoId
                WHERE p.UsuarioId = @usuarioId
                ORDER BY i.Id
                """,
                new { usuarioId },
                transacao
            )
        ).ToList();

        await transacao.CommitAsync();

        return Ok(new
        {
            Pedidos = pedidos,
            ItensPedido = itens
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhes(int id)
    {
        await using var conexao = await banco.AbrirAsync();

        var pedido =
            await conexao.QuerySingleOrDefaultAsync<Pedidos>(
                """
                SELECT * FROM Pedidos
                WHERE Id = @id AND UsuarioId = @usuarioId
                """,
                new
                {
                    id,
                    usuarioId = Regras.UsuarioId(User)
                }
            );

        if (pedido is null)
        {
            return NotFound(new
            {
                Mensagem = "Pedido não encontrado."
            });
        }

        var itens = await conexao.QueryAsync<ItensPedido>(
            """
            SELECT * FROM ItensPedido
            WHERE PedidoId = @id
            ORDER BY Id
            """,
            new { id }
        );

        return Ok(new
        {
            Pedido = pedido,
            ItensPedido = itens
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar(PedidoRequest dados)
    {
        Regras.Endereco(dados);

        Regras.Exigir(
            dados.Itens.All(item => item is not null) &&
            dados.Itens
                .Select(item => item.ProdutoId)
                .Distinct()
                .Count() == dados.Itens.Count,
            "O pedido contém produtos repetidos ou itens inválidos."
        );

        var usuarioId = Regras.UsuarioId(User);

        await using var conexao = await banco.AbrirAsync();
        await using var transacao =
            await conexao.BeginTransactionAsync();

        var usuarioExiste =
            await conexao.QuerySingleOrDefaultAsync<int?>(
                """
                SELECT Id FROM Usuarios
                WHERE Id = @usuarioId
                FOR UPDATE
                """,
                new { usuarioId },
                transacao
            );

        Regras.Exigir(
            usuarioExiste.HasValue,
            "Usuário não encontrado.",
            401
        );

        var existente =
            await conexao.QuerySingleOrDefaultAsync<Pedidos>(
                """
                SELECT * FROM Pedidos
                WHERE ChaveLocal = @ChaveLocal
                FOR UPDATE
                """,
                new { dados.ChaveLocal },
                transacao
            );

        if (existente is not null)
        {
            Regras.Exigir(
                existente.UsuarioId == usuarioId,
                "Chave de pedido já utilizada.",
                409
            );

            var itensAntigos =
                await conexao.QueryAsync<ItensPedido>(
                    """
                    SELECT * FROM ItensPedido
                    WHERE PedidoId = @Id
                    ORDER BY Id
                    """,
                    new { existente.Id },
                    transacao
                );

            await transacao.CommitAsync();

            return Ok(new
            {
                Pedido = existente,
                ItensPedido = itensAntigos
            });
        }

        var itens = new List<ItensPedido>();
        long total = 0;

        foreach (var escolha in dados.Itens.OrderBy(i => i.ProdutoId))
        {
            var produto =
                await conexao.QuerySingleOrDefaultAsync<Produtos>(
                    """
                    SELECT * FROM Produtos
                    WHERE Id = @ProdutoId
                    FOR SHARE
                    """,
                    new { escolha.ProdutoId },
                    transacao
                );

            Regras.Exigir(
                produto is { Ativo: true },
                "Um produto não está mais disponível. Atualize o catálogo.",
                409
            );

            Regras.Exigir(
                produto!.PrecoCentavos ==
                    escolha.PrecoUnitarioCentavos,
                $"O preço de {produto.Produto} mudou. " +
                "Atualize o catálogo e confira o carrinho.",
                409
            );

            var subtotal = Regras.Valor(
                (decimal)produto.PrecoCentavos *
                escolha.Quantidade
            );

            total = Regras.Valor(
                (decimal)total + subtotal
            );

            itens.Add(new ItensPedido
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Produto,
                Quantidade = escolha.Quantidade,
                PrecoUnitarioCentavos = produto.PrecoCentavos,
                SubtotalCentavos = subtotal
            });
        }

        var pedido = new Pedidos
        {
            ChaveLocal = dados.ChaveLocal,
            UsuarioId = usuarioId,
            DataHora = DateTime.UtcNow,
            Status = "Pedido Realizado",
            TotalCentavos = total,

            FormaPagamento = dados.FormaPagamento,
            FormaRecebimento = dados.FormaRecebimento,

            CEP = dados.CEP,
            Rua = dados.Rua,
            Bairro = dados.Bairro,
            Numero = dados.Numero,
            Complemento = dados.Complemento
        };

        pedido.Id = await conexao.ExecuteScalarAsync<int>(
            """
            INSERT INTO Pedidos
            (
                ChaveLocal, UsuarioId, DataHora, Status,
                TotalCentavos, FormaPagamento, FormaRecebimento,
                CEP, Rua, Bairro, Numero, Complemento
            )
            VALUES
            (
                @ChaveLocal, @UsuarioId, @DataHora, @Status,
                @TotalCentavos, @FormaPagamento, @FormaRecebimento,
                @CEP, @Rua, @Bairro, @Numero, @Complemento
            );

            SELECT LAST_INSERT_ID();
            """,
            pedido,
            transacao
        );

        foreach (var item in itens)
        {
            item.PedidoId = pedido.Id;

            item.Id = await conexao.ExecuteScalarAsync<int>(
                """
                INSERT INTO ItensPedido
                (
                    PedidoId, ProdutoId, NomeProduto, Quantidade,
                    PrecoUnitarioCentavos, SubtotalCentavos
                )
                VALUES
                (
                    @PedidoId, @ProdutoId, @NomeProduto, @Quantidade,
                    @PrecoUnitarioCentavos, @SubtotalCentavos
                );

                SELECT LAST_INSERT_ID();
                """,
                item,
                transacao
            );
        }

        if (dados.FormaRecebimento == Regras.Entrega)
        {
            await conexao.ExecuteAsync(
                """
                UPDATE Usuarios
                SET CEP = @CEP,
                    Rua = @Rua,
                    Bairro = @Bairro,
                    Numero = @Numero,
                    Complemento = @Complemento
                WHERE Id = @usuarioId
                """,
                new
                {
                    usuarioId,
                    dados.CEP,
                    dados.Rua,
                    dados.Bairro,
                    dados.Numero,
                    dados.Complemento
                },
                transacao
            );
        }

        await transacao.CommitAsync();

        return Created(
            $"/api/pedidos/{pedido.Id}",
            new
            {
                Pedido = pedido,
                ItensPedido = itens
            }
        );
    }
}