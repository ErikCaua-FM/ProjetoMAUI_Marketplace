using VanderShop.Models;

namespace VanderShop.Views;

public partial class frmCarrinho : ContentPage
{
    private readonly string _chaveLocal =
        Guid.NewGuid().ToString("N");

    private bool _carregando;

    private bool _salvando;

    private bool _finalizado;

    private bool _enderecoPreenchido;

    private bool _pronto;

    public frmCarrinho()
    {
        InitializeComponent();

        pickerPagamento.ItemsSource =
            OpcoesCompra.Pagamentos;

        pickerRecebimento.ItemsSource =
            OpcoesCompra.Recebimentos;

        pickerRecebimento.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_carregando || _salvando || _finalizado)
            return;

        _carregando = true;

        conteudo.IsEnabled = _pronto && !_finalizado && !App.CompraPendente;

        try
        {
            if (App.UsuarioLogado == null)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            if (!_enderecoPreenchido)
            {
                var usuario =
                    await App.DbInstance.ObterUsuarioAsync(
                        App.UsuarioLogado.Id);

                if (usuario == null)
                {
                    throw new InvalidOperationException(
                        "Usuário não encontrado.");
                }

                txtCEP.Text = usuario.CEP;
                txtRua.Text = usuario.Rua;
                txtBairro.Text = usuario.Bairro;
                txtNumero.Text = usuario.Numero;
                txtComplemento.Text = usuario.Complemento;

                _enderecoPreenchido = true;
            }

            if (App.DadosCompraPendente is { } pendente)
            {
                pickerPagamento.SelectedItem = pendente.FormaPagamento;
                pickerRecebimento.SelectedItem = pendente.FormaRecebimento;
                txtCEP.Text = pendente.CEP;
                txtRua.Text = pendente.Rua;
                txtBairro.Text = pendente.Bairro;
                txtNumero.Text = pendente.Numero;
                txtComplemento.Text = pendente.Complemento;
            }

            _pronto = true;

            AtualizarCarrinho();
        }
        catch (Exception ex)
        {
            _pronto = false;

            await DisplayAlertAsync(
                "Não foi possível carregar o carrinho",
                ex.Message,
                "OK");
        }
        finally
        {
            _carregando = false;

            conteudo.IsEnabled =
                _pronto && !_finalizado;
        }
    }

    private void AtualizarCarrinho()
    {
        BindableLayout.SetItemsSource(
            listaCarrinho,
            App.Carrinho.ToList());

        lblVazio.IsVisible =
            App.Carrinho.Count == 0;

        decimal total = App.Carrinho.Sum(
            item => item.Subtotal);

        lblTotal.Text =
            $"Total: {Dinheiro.Formatar(total)}";

        btnFinalizar.IsEnabled =
            _pronto &&
            !_salvando &&
            !_finalizado &&
            (App.Carrinho.Count > 0 || App.CompraPendente);

                btnFinalizar.Text = App.CompraPendente
                    ? "Tentar novamente"
                    : "Finalizar Compra";
    }

    private void Recebimento_Changed(
        object sender,
        EventArgs e)
    {
        camposEndereco.IsVisible =
            pickerRecebimento.SelectedItem as string
            == OpcoesCompra.Entrega;
    }

    private async void Aumentar_Clicked(object sender, EventArgs e)
    {
        if (_carregando || _salvando || _finalizado || !_pronto || App.CompraPendente)
            return;

        if (sender is not Button botao ||
            botao.BindingContext is not ItemCarrinho item)
        {
            return;
        }

        try
        {
            item.Quantidade = checked(item.Quantidade + 1);
            AtualizarCarrinho();
        }
        catch (OverflowException)
        {
            await DisplayAlertAsync(
                "Quantidade inválida",
                "A quantidade ultrapassa o valor numérico suportado pelo aplicativo.",
                "OK");
        }
    }

    private void Diminuir_Clicked(
        object sender,
        EventArgs e)
    {
        if (_salvando || _finalizado || App.CompraPendente)
            return;

        if (sender is Button botao &&
            botao.BindingContext is ItemCarrinho item)
        {
            if (item.Quantidade > 1)
            {
                item.Quantidade--;

                AtualizarCarrinho();
            }
        }
    }

    private void Remover_Clicked(
        object sender,
        EventArgs e)
    {
        if (_salvando || _finalizado)
            return;

        if (sender is Button botao &&
            botao.BindingContext is ItemCarrinho item)
        {
            App.Carrinho.Remove(item);

            AtualizarCarrinho();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        return _salvando || base.OnBackButtonPressed();
    }

    private async void Finalizar_Clicked(object sender, EventArgs e)
    {
        if (_carregando || _salvando || _finalizado || !_pronto)
            return;

        _salvando = true;
        conteudo.IsEnabled = _pronto && !_finalizado && !App.CompraPendente;
        btnFinalizar.IsEnabled = false;

        NavigationPage.SetHasBackButton(this, false);

        try
        {
            var usuario = App.UsuarioLogado;

            if (usuario == null)
                throw new Exception("Faça login para finalizar a compra.");

            var pedido = new Pedidos
            {
                ChaveLocal = _chaveLocal,
                UsuarioId = usuario.Id,

                FormaPagamento =
                    pickerPagamento.SelectedItem as string ?? "",

                FormaRecebimento =
                    pickerRecebimento.SelectedItem as string ?? "",

                CEP = txtCEP.Text?.Trim() ?? "",
                Rua = txtRua.Text?.Trim() ?? "",
                Bairro = txtBairro.Text?.Trim() ?? "",
                Numero = txtNumero.Text?.Trim() ?? "",
                Complemento = txtComplemento.Text?.Trim() ?? ""
            };

            int pedidoId = await App.SalvarPedidoAsync(
                pedido,
                App.Carrinho);

            _finalizado = true;

            App.Carrinho.Clear();
            AtualizarCarrinho();

            await DisplayAlertAsync(
                "Pedido realizado",
                $"Seu pedido #{pedidoId} foi realizado com sucesso!\n{App.AvisoCache}",
                "OK");

            await Navigation.PopToRootAsync();
        }
        catch (Exception ex)
        {
            string titulo = _finalizado
                ? "Pedido já salvo"
                : "Não foi possível finalizar";

            string mensagem = _finalizado
                ? "Seu pedido foi salvo, mas ocorreu um problema ao " +
                  $"retornar à tela inicial.\n{ex.Message}"
                : ex.Message;

            if (App.CompraPendente)
                mensagem += "\nUse Tentar novamente para confirmar a mesma compra.";

            await DisplayAlertAsync(titulo, mensagem, "OK");

            if (App.UsuarioLogado == null)
                await Navigation.PopToRootAsync();
        }
        finally
        {
            _salvando = false;

            conteudo.IsEnabled = _pronto && !_finalizado;

            NavigationPage.SetHasBackButton(this, true);

            AtualizarCarrinho();
        }
    }
}