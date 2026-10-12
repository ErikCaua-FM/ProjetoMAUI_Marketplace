using MySqlConnector;

namespace VanderShop.Api.Infrastructure;

public sealed class Banco(IConfiguration config)
{
    public async Task<MySqlConnection> AbrirAsync()
    {
        var texto = config.GetConnectionString("VanderShop");

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new ApiException(
                503,
                "Configure a conexão VanderShop na API."
            );
        }

        var builder = new MySqlConnectionStringBuilder(texto)
        {
            DateTimeKind = MySqlDateTimeKind.Utc
        };

        var connection = new MySqlConnection(
            builder.ConnectionString
        );

        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}