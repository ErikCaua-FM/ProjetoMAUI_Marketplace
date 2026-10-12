using System;
using System.Collections.Generic;
using System.Text;

namespace VanderShop.Models;

public class PedidosResposta
{
    public List<Pedidos> Pedidos { get; set; } = new();
    public List<ItensPedido> ItensPedido { get; set; } = new();
}