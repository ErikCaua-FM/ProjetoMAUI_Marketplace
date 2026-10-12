using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace VanderShop.Models;

[Table("Usuarios")]
public class Usuarios
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    [Unique]
    public string Documento { get; set; } = "";

    public string Telefone { get; set; } = "";

    [Unique]
    public string Email { get; set; } = "";

    public string CEP { get; set; } = "";

    public string Rua { get; set; } = "";

    public string Bairro { get; set; } = "";

    public string Numero { get; set; } = "";

    public string Complemento { get; set; } = "";
}