namespace VanderShop.Api.Models;

public class Produtos
{
    public int Id { get; set; }

    public string Produto { get; set; } = "";
    public string Descricao { get; set; } = "";

    public int CategoriaId { get; set; }

    public long PrecoCentavos { get; set; }

    public bool Ativo { get; set; }
}