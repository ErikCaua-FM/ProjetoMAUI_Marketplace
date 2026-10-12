using System;
using System.Collections.Generic;
using System.Text;

namespace VanderShop.Models;

public class PedidoResposta
{
    public Pedidos Pedido { get; set; } = new();
    public List<ItensPedido> ItensPedido { get; set; } = new();
}