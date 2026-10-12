using System.Security.Claims;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VanderShop.Api.Infrastructure;
using VanderShop.Api.Models;
using VanderShop.Api.Requests;

namespace VanderShop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    Banco banco,
    IConfiguration config) : ControllerBase
{
    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastrar(
        CadastroRequest dados)
    {
        dados.Nome = dados.Nome.Trim();
        dados.Email = dados.Email.Trim().ToLowerInvariant();

        dados.Documento = Regex.Replace(
            dados.Documento,
            "[^a-zA-Z0-9]",
            ""
        ).ToUpperInvariant();

        dados.Telefone = Regras.Digitos(dados.Telefone);

        Regras.Exigir(
            dados.Nome.Length >= 3 &&
            dados.Documento.Length is >= 3 and <= 20 &&
            dados.Telefone.Length is >= 10 and <= 15,
            "Confira nome, documento e telefone."
        );

        await using var conexao = await banco.AbrirAsync();

        var id = await conexao.ExecuteScalarAsync<int>(
            """
            INSERT INTO Usuarios
                (Nome, Documento, Telefone, Email, Senha)
            VALUES
                (@Nome, @Documento, @Telefone, @Email, @Senha);

            SELECT LAST_INSERT_ID();
            """,
            dados
        );

        var usuario = await conexao.QuerySingleAsync<Usuarios>(
            "SELECT * FROM Usuarios WHERE Id = @id",
            new { id }
        );

        return Created("/api/auth/me", usuario);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest dados)
    {
        await using var conexao = await banco.AbrirAsync();

        var usuario =
            await conexao.QuerySingleOrDefaultAsync<Usuarios>(
                "SELECT * FROM Usuarios WHERE Email = @Email",
                new
                {
                    Email = dados.Email.Trim().ToLowerInvariant()
                }
            );

        Regras.Exigir(
            usuario is not null &&
            string.Equals(
                usuario.Senha,
                dados.Senha,
                StringComparison.Ordinal
            ),
            "E-mail ou senha incorretos.",
            401
        );

        return CriarToken(
            usuario!.Id.ToString(),
            usuario.Nome,
            "Cliente"
        );
    }

    [HttpPost("admin/login")]
    public IActionResult LoginAdmin(LoginRequest dados)
    {
        var email = config["Administrador:Email"];
        var senha = config["Administrador:Senha"];

        Regras.Exigir(
            !string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(senha),
            "Configure o acesso administrativo na API.",
            503
        );

        Regras.Exigir(
            string.Equals(
                email,
                dados.Email.Trim(),
                StringComparison.OrdinalIgnoreCase
            ) &&
            string.Equals(
                senha,
                dados.Senha,
                StringComparison.Ordinal
            ),
            "E-mail ou senha incorretos.",
            401
        );

        return CriarToken(
            "administrador",
            "Administração",
            "Administrador"
        );
    }

    [Authorize(Roles = "Cliente")]
    [HttpGet("me")]
    public async Task<Usuarios> Me()
    {
        await using var conexao = await banco.AbrirAsync();

        var usuario =
            await conexao.QuerySingleOrDefaultAsync<Usuarios>(
                "SELECT * FROM Usuarios WHERE Id = @id",
                new { id = Regras.UsuarioId(User) }
            );

        return usuario ?? throw new ApiException(
            401,
            "Usuário não encontrado."
        );
    }

    private SignInResult CriarToken(
        string id,
        string nome,
        string perfil)
    {
        var identidade = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Role, perfil)
            },
            BearerTokenDefaults.AuthenticationScheme
        );

        return SignIn(
            new ClaimsPrincipal(identidade),
            BearerTokenDefaults.AuthenticationScheme
        );
    }
}