namespace VanderShop.Api.Infrastructure;

public sealed class ApiException(int status, string mensagem)
    : Exception(mensagem)
{
    public int Status { get; } = status;
}