using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace VanderShop.Models;

[Table("ItensPedido")]
public class ItensPedido
{
    [PrimaryKey]
    public int Id { get; set; }

    [Indexed]
    public int PedidoId { get; set; }

    [Indexed]
    public int? ProdutoId { get; set; }

    public string NomeProduto { get; set; } = "";

    public int Quantidade { get; set; }

    public long PrecoUnitarioCentavos { get; set; }

    public long SubtotalCentavos { get; set; }

    [Ignore]
    public decimal PrecoUnitario =>
        PrecoUnitarioCentavos / 100m;

    [Ignore]
    public decimal Subtotal =>
        SubtotalCentavos / 100m;

    [Ignore]
    public string PrecoUnitarioTexto =>
        Dinheiro.Formatar(PrecoUnitario);

    [Ignore]
    public string SubtotalTexto =>
        Dinheiro.Formatar(Subtotal);
}