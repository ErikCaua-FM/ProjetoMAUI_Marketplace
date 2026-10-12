using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderShop.Api.Infrastructure;
using VanderShop.Api.Models;
using VanderShop.Api.Requests;

namespace VanderShop.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/admin")]
public class AdminController(Banco banco) : ControllerBase
{
    [HttpGet("painel")]
    public async Task<IActionResult> Painel()
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao =
            await conexao.BeginTransactionAsync();

        using var dados = await conexao.QueryMultipleAsync(
            """
            SELECT * FROM Categorias ORDER BY Categoria;
            SELECT * FROM Produtos ORDER BY Produto;
            SELECT Id, Nome, Email FROM Usuarios;
            SELECT * FROM Pedidos ORDER BY DataHora DESC, Id DESC;
            SELECT * FROM ItensPedido ORDER BY Id;
            """,
            transaction: transacao
        );

        var categorias =
            (await dados.ReadAsync<Categorias>()).ToList();

        var produtos =
            (await dados.ReadAsync<Produtos>()).ToList();

        var usuarios =
            (await dados.ReadAsync()).ToList();

        var pedidos =
            (await dados.ReadAsync<Pedidos>()).ToList();

        var itens =
            (await dados.ReadAsync<ItensPedido>()).ToList();

        await transacao.CommitAsync();

        return Ok(new
        {
            Categorias = categorias,
            Produtos = produtos,
            Usuarios = usuarios,
            Pedidos = pedidos,
            ItensPedido = itens
        });
    }

    [HttpGet("pedidos")]
    public async Task<IEnumerable<dynamic>> Buscar(
        [FromQuery] string? pesquisa)
    {
        await using var conexao = await banco.AbrirAsync();

        var busca = (pesquisa ?? "").Trim();

        return await conexao.QueryAsync(
            """
            SELECT p.*, u.Nome AS NomeCliente
            FROM Pedidos p
            INNER JOIN Usuarios u ON u.Id = p.UsuarioId
            WHERE @busca = ''
               OR LOCATE(@busca, CAST(p.Id AS CHAR)) > 0
               OR LOCATE(@busca, u.Nome) > 0
               OR LOCATE(
                    @busca,
                    p.Status COLLATE utf8mb4_0900_ai_ci
                  ) > 0
            ORDER BY p.DataHora DESC, p.Id DESC
            """,
            new { busca }
        );
    }

    [HttpPut("pedidos/{id:int}/status")]
    public async Task<Pedidos> AlterarStatus(
        int id,
        StatusRequest dados)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao =
            await conexao.BeginTransactionAsync();

        var pedido =
            await conexao.QuerySingleOrDefaultAsync<Pedidos>(
                """
                SELECT * FROM Pedidos
                WHERE Id = @id
                FOR UPDATE
                """,
                new { id },
                transacao
            )
            ?? throw new ApiException(
                404,
                "Pedido não encontrado."
            );

        Regras.Exigir(
            pedido.Status == dados.StatusAnterior,
            "O status foi alterado por outra operação. " +
            "Reabra os detalhes.",
            409
        );

        if (dados.Status != pedido.Status)
        {
            Regras.Exigir(
                Regras.Proximos(pedido).Contains(dados.Status),
                "Essa alteração de status não é permitida para este pedido.",
                409
            );

            await conexao.ExecuteAsync(
                """
                UPDATE Pedidos
                SET Status = @Status
                WHERE Id = @id
                """,
                new { id, dados.Status },
                transacao
            );

            pedido.Status = dados.Status;
        }

        await transacao.CommitAsync();

        return pedido;
    }
}