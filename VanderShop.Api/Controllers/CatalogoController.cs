using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderShop.Api.Infrastructure;
using VanderShop.Api.Models;
using VanderShop.Api.Requests;

namespace VanderShop.Api.Controllers;

[ApiController]
[Route("api")]
public class CatalogoController(Banco banco) : ControllerBase
{
    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalogo()
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao =
            await conexao.BeginTransactionAsync();

        var categorias = (
            await conexao.QueryAsync<Categorias>(
                "SELECT * FROM Categorias ORDER BY Categoria",
                transaction: transacao
            )
        ).ToList();

        var produtos = (
            await conexao.QueryAsync<Produtos>(
                """
                SELECT * FROM Produtos
                WHERE Ativo = 1
                ORDER BY Produto
                """,
                transaction: transacao
            )
        ).ToList();

        await transacao.CommitAsync();

        return Ok(new
        {
            Categorias = categorias,
            Produtos = produtos
        });
    }

    [HttpGet("categorias/{id:int}")]
    public async Task<Categorias> Categoria(int id)
    {
        await using var conexao = await banco.AbrirAsync();

        return await conexao.QuerySingleOrDefaultAsync<Categorias>(
            "SELECT * FROM Categorias WHERE Id = @id",
            new { id }
        ) ?? throw new ApiException(
            404,
            "Categoria não encontrada."
        );
    }

    [HttpGet("produtos/{id:int}")]
    public async Task<Produtos> Produto(int id)
    {
        await using var conexao = await banco.AbrirAsync();

        return await conexao.QuerySingleOrDefaultAsync<Produtos>(
            """
            SELECT * FROM Produtos
            WHERE Id = @id AND (Ativo = 1 OR @admin = 1)
            """,
            new
            {
                id,
                admin = User.IsInRole("Administrador")
            }
        ) ?? throw new ApiException(
            404,
            "Produto não encontrado."
        );
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria(
        CategoriaRequest dados)
    {
        dados.Categoria = dados.Categoria.Trim();

        Regras.Exigir(
            dados.Categoria.Length >= 2,
            "Informe o nome da categoria."
        );

        await using var conexao = await banco.AbrirAsync();

        var id = await conexao.ExecuteScalarAsync<int>(
            """
            INSERT INTO Categorias (Categoria)
            VALUES (@Categoria);

            SELECT LAST_INSERT_ID();
            """,
            dados
        );

        return Created(
            $"/api/categorias/{id}",
            new Categorias
            {
                Id = id,
                Categoria = dados.Categoria
            }
        );
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("produtos")]
    public async Task<IActionResult> CriarProduto(
        ProdutoRequest dados)
    {
        Normalizar(dados);

        await using var conexao = await banco.AbrirAsync();

        var id = await conexao.ExecuteScalarAsync<int>(
            """
            INSERT INTO Produtos
                (Produto, Descricao, CategoriaId, PrecoCentavos, Ativo)
            VALUES
                (@Produto, @Descricao, @CategoriaId, @PrecoCentavos, @Ativo);

            SELECT LAST_INSERT_ID();
            """,
            dados
        );

        var produto = await conexao.QuerySingleAsync<Produtos>(
            "SELECT * FROM Produtos WHERE Id = @id",
            new { id }
        );

        return Created($"/api/produtos/{id}", produto);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("produtos/{id:int}")]
    public async Task<Produtos> AlterarProduto(
        int id,
        ProdutoRequest dados)
    {
        Normalizar(dados);

        await using var conexao = await banco.AbrirAsync();

        var alteradas = await conexao.ExecuteAsync(
            """
            UPDATE Produtos
            SET Produto = @Produto,
                Descricao = @Descricao,
                CategoriaId = @CategoriaId,
                PrecoCentavos = @PrecoCentavos,
                Ativo = @Ativo
            WHERE Id = @id
            """,
            new
            {
                id,
                dados.Produto,
                dados.Descricao,
                dados.CategoriaId,
                dados.PrecoCentavos,
                dados.Ativo
            }
        );

        Regras.Exigir(
            alteradas > 0,
            "Produto não encontrado.",
            404
        );

        return await conexao.QuerySingleAsync<Produtos>(
            "SELECT * FROM Produtos WHERE Id = @id",
            new { id }
        );
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("produtos/{id:int}")]
    public async Task<IActionResult> ExcluirProduto(int id)
    {
        await using var conexao = await banco.AbrirAsync();

        var excluidas = await conexao.ExecuteAsync(
            "DELETE FROM Produtos WHERE Id = @id",
            new { id }
        );

        Regras.Exigir(
            excluidas > 0,
            "Produto não encontrado.",
            404
        );

        return NoContent();
    }

    private static void Normalizar(ProdutoRequest dados)
    {
        dados.Produto = dados.Produto.Trim();
        dados.Descricao = dados.Descricao.Trim();

        Regras.Exigir(
            dados.Produto.Length >= 2 &&
            dados.Descricao.Length > 0,
            "Informe o nome e a descrição do produto."
        );
    }
}