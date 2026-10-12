namespace VanderShop.Api.Models;

public class ItensPedido
{
    public int Id { get; set; }

    public int PedidoId { get; set; }

    public int? ProdutoId { get; set; }

    public string NomeProduto { get; set; } = "";

    public int Quantidade { get; set; }

    public long PrecoUnitarioCentavos { get; set; }

    public long SubtotalCentavos { get; set; }
}