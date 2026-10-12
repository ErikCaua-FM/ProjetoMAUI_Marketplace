using VanderShop.Models;

namespace VanderShop.Views;

public partial class frmPedidos : ContentPage
{
    private bool _carregando;
    private bool _navegando;
    private bool _pronto;

    public frmPedidos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        if (_carregando)
            return;

        _carregando = true;

        btnNovoPedido.IsEnabled = false;
        btnSair.IsEnabled = false;
        listaPedidos.IsEnabled = false;

        try
        {
            await App.InicializarAsync();

            if (App.UsuarioLogado != null)
            {
                try
                {
                    await App.AtualizarPedidosAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync(
                        "Não foi possível atualizar os pedidos",
                        ex.Message,
                        "OK");
                }
            }

            var usuario = App.UsuarioLogado;

            lblUsuario.Text = usuario == null
                ? "Bem-vindo à VanderShop"
                : $"Olá, {usuario.Nome}!";

            btnSair.IsVisible = usuario != null;

            listaPedidos.ItemsSource = usuario == null
                ? new List<Pedidos>()
                : await App.DbInstance.ObterPedidosAsync(
                    usuario.Id);

            _pronto = true;
        }
        catch (Exception ex)
        {
            _pronto = false;

            await DisplayAlertAsync(
                "Não foi possível carregar",
                ex.Message,
                "OK");
        }
        finally
        {
            _carregando = false;

            refreshPedidos.IsRefreshing = false;

            btnNovoPedido.IsEnabled = _pronto;
            btnSair.IsEnabled = _pronto;
            listaPedidos.IsEnabled = _pronto;
        }
    }

    private async Task AbrirPaginaAsync(Page pagina)
    {
        if (_navegando || !_pronto || _carregando)
            return;

        _navegando = true;
        btnNovoPedido.IsEnabled = false;

        try
        {
            await Navigation.PushAsync(pagina);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível abrir a tela",
                ex.Message,
                "OK");
        }
        finally
        {
            _navegando = false;
            btnNovoPedido.IsEnabled = _pronto;
        }
    }

    private async void Pedidos_Refreshing(
        object sender,
        EventArgs e)
    {
        await CarregarAsync();
    }

    private async void NovoPedido_Clicked(
        object sender,
        EventArgs e)
    {
        if (App.UsuarioLogado == null)
        {
            await AbrirPaginaAsync(new frmLogin());
        }
        else
        {
            await AbrirPaginaAsync(new frmListaProdutos());
        }
    }

    private async void Sair_Clicked(
        object sender,
        EventArgs e)
    {
        if (_navegando || _carregando || !_pronto)
            return;

        try
        {
            await App.SairAsync();

            await CarregarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro",
                ex.Message,
                "OK");
        }
    }

    private async void Detalhes_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is Button botao &&
            botao.BindingContext is Pedidos pedido)
        {
            await AbrirPaginaAsync(
                new frmDetalhesPedido(pedido.Id));
        }
    }
}