using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class CadastroRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nome { get; set; } = "";

    [Required, StringLength(30)]
    public string Documento { get; set; } = "";

    [Required, StringLength(30)]
    public string Telefone { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 8)]
    public string Senha { get; set; } = "";
}