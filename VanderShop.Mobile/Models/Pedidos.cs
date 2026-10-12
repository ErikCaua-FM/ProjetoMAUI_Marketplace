using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace VanderShop.Models;

[Table("Pedidos")]
public class Pedidos
{
    [PrimaryKey]
    public int Id { get; set; }

    [Unique]
    public string ChaveLocal { get; set; } =
        Guid.NewGuid().ToString("N");

    [Indexed]
    public int UsuarioId { get; set; }

    public DateTime DataHora { get; set; }

    public string Status { get; set; } =
        StatusPedido.Realizado;

    public long TotalCentavos { get; set; }

    public string FormaPagamento { get; set; } = "";

    public string FormaRecebimento { get; set; } = "";

    public string CEP { get; set; } = "";

    public string Rua { get; set; } = "";

    public string Bairro { get; set; } = "";

    public string Numero { get; set; } = "";

    public string Complemento { get; set; } = "";

    [Ignore]
    public decimal PrecoTotal => TotalCentavos / 100m;

    [Ignore]
    public string Titulo => $"Pedido #{Id}";

    [Ignore]
    public string DataHoraTexto =>
        DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    [Ignore]
    public string TotalTexto =>
        Dinheiro.Formatar(PrecoTotal);

    [Ignore]
    public string EnderecoTexto
    {
        get
        {
            if (FormaRecebimento == OpcoesCompra.Retirada)
                return "Retirada na loja.";

            string complemento =
                string.IsNullOrWhiteSpace(Complemento)
                    ? ""
                    : $" — {Complemento}";

            return $"{Rua}, {Numero}{complemento}\n" +
                   $"{Bairro} — CEP: {CEP}";
        }
    }
}