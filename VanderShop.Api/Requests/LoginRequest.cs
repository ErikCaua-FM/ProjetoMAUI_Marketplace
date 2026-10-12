using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class LoginRequest
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(128)]
    public string Senha { get; set; } = "";
}