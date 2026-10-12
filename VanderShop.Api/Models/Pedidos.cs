namespace VanderShop.Api.Models;

public class Pedidos
{
    public int Id { get; set; }

    public string ChaveLocal { get; set; } = "";

    public int UsuarioId { get; set; }

    public DateTime DataHora { get; set; }

    public string Status { get; set; } = "";

    public long TotalCentavos { get; set; }

    public string FormaPagamento { get; set; } = "";
    public string FormaRecebimento { get; set; } = "";

    public string CEP { get; set; } = "";
    public string Rua { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
}