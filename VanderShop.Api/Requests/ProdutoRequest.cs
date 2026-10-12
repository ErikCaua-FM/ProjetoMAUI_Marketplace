using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class ProdutoRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Produto { get; set; } = "";

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Descricao { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    [Range(typeof(long), "0", "9007199254740991")]
    public long PrecoCentavos { get; set; }

    public bool Ativo { get; set; } = true;
}