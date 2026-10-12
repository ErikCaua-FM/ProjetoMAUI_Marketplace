using SQLite;

namespace VanderShop.Models
{
    [Table("Produtos")]
    public class Produtos
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Produto { get; set; } = "";

        public string Descricao { get; set; } = "";

        public bool Ativo { get; set; } = true;

        [Indexed]
        public int CategoriaId { get; set; }

        [Ignore]
        public Categorias aCategoria { get; set; } = new Categorias();

        public long PrecoCentavos { get; set; }

        [Ignore]
        public decimal Preco
        {
            get => PrecoCentavos / 100m;
            set => PrecoCentavos = Dinheiro.ParaCentavos(value);
        }

        [Ignore]
        public string PrecoTexto => Dinheiro.Formatar(Preco);
    }
}