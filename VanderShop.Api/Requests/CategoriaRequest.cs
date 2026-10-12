using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class CategoriaRequest
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string Categoria { get; set; } = "";
}