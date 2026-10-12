using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace VanderShop.Models;

[Table("Categorias")]
public class Categorias
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Categoria { get; set; } = "";
}