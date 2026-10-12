using Dapper;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using VanderShop.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Banco>();

builder.Services.ConfigureHttpJsonOptions(opcoes =>
{
    opcoes.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.Configure<ApiBehaviorOptions>(opcoes =>
{
    opcoes.InvalidModelStateResponseFactory = contexto =>
        new BadRequestObjectResult(new
        {
            Mensagem = "Confira os campos informados.",

            Erros = contexto.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors
                        .Select(erro => erro.ErrorMessage)
                        .ToArray()
                )
        });
});

builder.Services
    .AddAuthentication(
        BearerTokenDefaults.AuthenticationScheme
    )
    .AddBearerToken(opcoes =>
    {
        opcoes.BearerTokenExpiration = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opcoes =>
{
    opcoes.AddDefaultPolicy(politica =>
    {
        var origens = builder.Configuration
            .GetSection("Cors:Origens")
            .Get<string[]>() ?? [];

        politica
            .WithOrigins(origens)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo(contexto);
    }
    catch (Exception erro) when (!contexto.Response.HasStarted)
    {
        var (status, mensagem) = erro switch
        {
            ApiException erroApi =>
                (erroApi.Status, erroApi.Message),

            MySqlException { Number: 1062 } =>
                (
                    409,
                    "Já existe um registro com esses dados " +
                    "(e-mail, documento, categoria ou chave do pedido)."
                ),

            MySqlException { Number: 1452 } =>
                (
                    400,
                    "O registro relacionado não existe mais. " +
                    "Atualize os dados."
                ),

            MySqlException { Number: 1451 } =>
                (
                    409,
                    "Este registro está vinculado a outros dados " +
                    "e não pode ser excluído."
                ),

            MySqlException { Number: 3819 or 1406 or 1048 } =>
                (
                    400,
                    "Os dados não atendem às regras do banco."
                ),

            MySqlException =>
                (
                    503,
                    "Não foi possível acessar o banco. " +
                    "Verifique o Docker e a conexão da API."
                ),

            _ =>
                (
                    500,
                    "Não foi possível concluir a operação."
                )
        };

        if (status >= 500)
            app.Logger.LogError(erro, "Falha na API");

        contexto.Response.StatusCode = status;

        await contexto.Response.WriteAsJsonAsync(new
        {
            Mensagem = mensagem
        });
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/saude", async (Banco banco) =>
{
    await using var conexao = await banco.AbrirAsync();

    var tabelas = await conexao.ExecuteScalarAsync<int>(
        """
        SELECT COUNT(*)
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME IN
          (
              'Usuarios',
              'Categorias',
              'Produtos',
              'Pedidos',
              'ItensPedido'
          )
        """
    );

    Regras.Exigir(
        tabelas == 5,
        "Execute o script 01-estrutura.sql no banco configurado.",
        503
    );

    return Results.Ok(new
    {
        Api = "OK",
        Banco = "OK",
        Tabelas = tabelas
    });
});

app.Run();