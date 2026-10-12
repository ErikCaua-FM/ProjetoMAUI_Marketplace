using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VanderShop.Models;

namespace VanderShop.Helpers;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string? _token;

    public ApiService(string? baseUrl = null)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl ?? ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public void LimparSessao() => _token = null;

    public Task<Usuarios> CadastrarAsync(Usuarios usuario, string senha)
    {
        var corpo = JsonSerializer.Serialize(new
        {
            usuario.Nome,
            usuario.Documento,
            usuario.Telefone,
            usuario.Email,
            Senha = senha
        });

        return EnviarAsync<Usuarios>(
            "auth/cadastro", HttpMethod.Post, corpo, false);
    }

    public async Task<Usuarios> EntrarAsync(string email, string senha)
    {
        LimparSessao();

        var corpo = JsonSerializer.Serialize(new
        {
            Email = email.Trim(),
            Senha = senha
        });

        var resposta = await EnviarAsync<TokenResposta>(
            "auth/login", HttpMethod.Post, corpo, false);

        if (string.IsNullOrWhiteSpace(resposta.AccessToken))
            throw new ApiException(0, "A API não retornou o token de acesso.");

        _token = resposta.AccessToken;
        return await ObterUsuarioAsync();
    }

    public Task<Usuarios> ObterUsuarioAsync() =>
        EnviarAsync<Usuarios>("auth/me", HttpMethod.Get);

    public Task<PedidosResposta> ObterPedidosAsync() =>
        EnviarAsync<PedidosResposta>("pedidos", HttpMethod.Get);

    public Task<CatalogoResposta> ObterCatalogoAsync() =>
        EnviarAsync<CatalogoResposta>(
            "catalogo", HttpMethod.Get, autenticado: false);

    public Task<PedidoResposta> SalvarPedidoAsync(string corpo) =>
        EnviarAsync<PedidoResposta>("pedidos", HttpMethod.Post, corpo);

    private async Task<T> EnviarAsync<T>(
        string caminho,
        HttpMethod metodo,
        string? corpo = null,
        bool autenticado = true)
    {
        using var requisicao = new HttpRequestMessage(metodo, caminho);

        if (autenticado)
        {
            if (string.IsNullOrWhiteSpace(_token))
                throw new ApiException(401, "Entre novamente na sua conta.");

            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        if (corpo is not null)
            requisicao.Content = new StringContent(
                corpo, Encoding.UTF8, "application/json");

        try
        {
            using var resposta = await _http.SendAsync(requisicao);
            var texto = await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
            {
                string mensagem = $"A API retornou o erro {(int)resposta.StatusCode}.";

                try
                {
                    using var documento = JsonDocument.Parse(texto);
                    var raiz = documento.RootElement;

                    if (raiz.TryGetProperty("Mensagem", out var detalhe))
                        mensagem = detalhe.GetString() ?? mensagem;

                    if (raiz.TryGetProperty("Erros", out var erros))
                    {
                        mensagem += " " + string.Join(" ", erros
                            .EnumerateObject()
                            .SelectMany(item => item.Value.EnumerateArray())
                            .Select(item => item.GetString()));
                    }
                }
                catch (JsonException) { }

                if (autenticado && resposta.StatusCode == HttpStatusCode.Unauthorized)
                    mensagem = "Sua sessão terminou. Entre novamente.";

                throw new ApiException((int)resposta.StatusCode, mensagem);
            }

            return JsonSerializer.Deserialize<T>(texto, _json)
                ?? throw new ApiException(0, "A API retornou uma resposta vazia.");
        }
        catch (Exception erro) when (
            erro is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(0,
                "Não foi possível acessar a API. Confira a conexão e tente novamente.");
        }
        catch (JsonException)
        {
            throw new ApiException(0, "A API retornou uma resposta inválida.");
        }
    }
}