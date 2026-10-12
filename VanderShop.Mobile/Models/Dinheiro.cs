using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace VanderShop.Models;

public static class Dinheiro
{
    public static long ParaCentavos(decimal valor)
    {
        if (valor < 0 || decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentException(
                "O valor deve ser positivo ou zero e ter até duas casas decimais.");
        }

        return checked((long)(valor * 100m));
    }

    public static string Formatar(decimal valor)
    {
        return valor.ToString(
            "C2",
            CultureInfo.GetCultureInfo("pt-BR"));
    }
}