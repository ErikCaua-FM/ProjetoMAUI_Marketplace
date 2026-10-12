using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VanderShop.Models
{
    public class ItemCarrinho : INotifyPropertyChanged
    {
        private int _quantidade;

        public Produtos oProduto { get; set; } = new Produtos();

        public int Quantidade
        {
            get => _quantidade;

            set
            {
                if (_quantidade == value)
                    return;

                _quantidade = value;


                OnPropertyChanged();

                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(SubtotalTexto));
            }
        }

        public decimal Subtotal =>
            oProduto.Preco * Quantidade;

        public string PrecoUnitarioTexto =>
            oProduto.PrecoTexto;

        public string SubtotalTexto =>
            Dinheiro.Formatar(Subtotal);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? nomePropriedade = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nomePropriedade));
        }
    }
}