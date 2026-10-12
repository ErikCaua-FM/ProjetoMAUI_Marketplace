using System.Text.Json.Serialization;

namespace VanderShop.Api.Models;

public class Usuarios
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Email { get; set; } = "";

    [JsonIgnore]
    public string Senha { get; set; } = "";

    public string CEP { get; set; } = "";
    public string Rua { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
}