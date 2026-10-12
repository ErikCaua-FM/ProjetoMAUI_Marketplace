namespace VanderShop.Views;

public partial class frmDetalhesPedido : ContentPage
{
    private readonly int _pedidoId;

    private bool _carregando;

    public frmDetalhesPedido(int pedidoId)
    {
        InitializeComponent();

        _pedidoId = pedidoId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_carregando)
            return;

        _carregando = true;

        try
        {
            var usuario = App.UsuarioLogado;

            if (usuario == null)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            var detalhes =
                await App.DbInstance.ObterDetalhesAsync(
                    _pedidoId,
                    usuario.Id);

            BindingContext = detalhes.Pedido;

            BindableLayout.SetItemsSource(
                listaItens,
                detalhes.Itens);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível carregar o pedido",
                ex.Message,
                "OK");
        }
        finally
        {
            _carregando = false;
        }
    }
}