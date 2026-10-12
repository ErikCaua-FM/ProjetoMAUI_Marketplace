using System;
using System.Collections.Generic;
using System.Text;

namespace VanderShop.Models;

public class CatalogoResposta
{
    public List<Categorias> Categorias { get; set; } = new();
    public List<Produtos> Produtos { get; set; } = new();
}