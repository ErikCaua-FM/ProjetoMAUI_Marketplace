using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class ItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProdutoId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantidade { get; set; }

    [Required, Range(typeof(long), "0", "9007199254740991")]
    public long? PrecoUnitarioCentavos { get; set; }
}