using System;
using System.Collections.Generic;
using System.Text;
namespace VanderShop.Models;

public static class OpcoesCompra
{
    public const string Retirada = "Retirar na loja";

    public const string Entrega = "Realizar uma entrega";

    public static readonly string[] Pagamentos =
    {
        "Pix",
        "Cartão de crédito",
        "Cartão de débito",
        "Boleto bancário"
    };

    public static readonly string[] Recebimentos =
    {
        Retirada,
        Entrega
    };
}