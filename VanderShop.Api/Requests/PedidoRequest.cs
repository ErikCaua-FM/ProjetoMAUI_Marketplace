using System.ComponentModel.DataAnnotations;

namespace VanderShop.Api.Requests;

public class PedidoRequest
{
    [Required, RegularExpression("^[0-9a-f]{32}$")]
    public string ChaveLocal { get; set; } = "";

    [Required]
    public string FormaPagamento { get; set; } = "";

    [Required]
    public string FormaRecebimento { get; set; } = "";

    [StringLength(9)]
    public string CEP { get; set; } = "";

    [StringLength(100)]
    public string Rua { get; set; } = "";

    [StringLength(60)]
    public string Bairro { get; set; } = "";

    [StringLength(10)]
    public string Numero { get; set; } = "";

    [StringLength(150)]
    public string Complemento { get; set; } = "";

    [Required, MinLength(1)]
    public List<ItemRequest> Itens { get; set; } = [];
}