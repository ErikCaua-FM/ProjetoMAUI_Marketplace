using VanderShop.Models;

namespace VanderShop.Views;

public partial class frmListaProdutos : ContentPage
{
    private List<Produtos> _produtos = new();

    private bool _carregando;

    private bool _ocupado;

    public frmListaProdutos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_carregando)
            return;

        _carregando = true;

        listaProdutos.IsEnabled = false;
        menuCarrinho.IsEnabled = false;

        try
        {
            if (App.UsuarioLogado == null)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            int categoriaAnterior =
                (pickerCategoria.SelectedItem as Categorias)?.Id
                ?? 0;

            try
            {
                await App.AtualizarCatalogoAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Não foi possível atualizar o catálogo",
                    ex.Message + "\nSerão usados os produtos já recebidos da API, se houver.",
                    "OK");
            }

            _produtos =
                await App.DbInstance.ObterProdutosAsync();

            var categorias =
                await App.DbInstance.ObterCategoriasAsync();

            categorias.Insert(0, new Categorias
            {
                Id = 0,
                Categoria = "Todas as categorias"
            });

            pickerCategoria.ItemsSource = categorias;

            pickerCategoria.SelectedItem =
                categorias.FirstOrDefault(
                    c => c.Id == categoriaAnterior)
                ?? categorias[0];

            AplicarFiltro();

            AtualizarIndicadorCarrinho();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível carregar os produtos",
                ex.Message,
                "OK");
        }
        finally
        {
            _carregando = false;

            listaProdutos.IsEnabled = true;
            menuCarrinho.IsEnabled = true;
        }
    }

    private void AplicarFiltro()
    {
        int categoriaId =
            (pickerCategoria.SelectedItem as Categorias)?.Id
            ?? 0;

        listaProdutos.ItemsSource = categoriaId == 0
            ? _produtos.ToList()
            : _produtos
                .Where(p => p.CategoriaId == categoriaId)
                .ToList();
    }

    private void Categoria_Changed(
        object sender,
        EventArgs e)
    {
        AplicarFiltro();
    }

    private void AtualizarIndicadorCarrinho()
    {
        long quantidade = App.Carrinho.Sum(
            item => (long)item.Quantidade);

        menuCarrinho.Text = $"Carrinho ({quantidade})";
    }

    private async void Produto_Selecionado(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_carregando || _ocupado)
            return;

        if (e.CurrentSelection.FirstOrDefault() is not Produtos produto)
            return;

        _ocupado = true;
        listaProdutos.SelectedItem = null;

        try
        {
            string? resposta = await DisplayPromptAsync(
                produto.Produto,
                $"Preço unitário: {produto.PrecoTexto}\n" +
                "Qual quantidade deseja adicionar?",
                accept: "Adicionar ao carrinho",
                cancel: "Cancelar",
                placeholder: "Quantidade",
                maxLength: -1,
                keyboard: Keyboard.Numeric,
                initialValue: "1");

            if (resposta == null)
                return;

            if (!int.TryParse(resposta, out int quantidade) ||
                quantidade <= 0)
            {
                if (App.CompraPendente)
                {
                    await DisplayAlertAsync(
                        "Compra aguardando confirmação",
                        "Volte ao carrinho e tente confirmar a compra já enviada.",
                        "OK");
                    return;
                }
                await DisplayAlertAsync(
                    "Quantidade inválida",
                    "Informe uma quantidade inteira válida e maior que zero.",
                    "OK");

                return;
            }

            var existente = App.Carrinho.FirstOrDefault(
                item => item.oProduto.Id == produto.Id);

            if (existente == null)
            {
                App.Carrinho.Add(new ItemCarrinho
                {
                    oProduto = produto,
                    Quantidade = quantidade
                });
            }
            else
            {
                existente.Quantidade = checked(
                    existente.Quantidade + quantidade);
            }

            AtualizarIndicadorCarrinho();

            await DisplayAlertAsync(
                "Adicionado ao carrinho",
                $"{quantidade} unidade(s) de {produto.Produto} adicionada(s).",
                "OK");
        }
        catch (OverflowException)
        {
            await DisplayAlertAsync(
                "Quantidade inválida",
                "A quantidade ultrapassa o valor numérico suportado pelo aplicativo.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível adicionar",
                ex.Message,
                "OK");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async void Carrinho_Clicked(
        object sender,
        EventArgs e)
    {
        if (_ocupado || _carregando)
            return;

        _ocupado = true;

        try
        {
            await Navigation.PushAsync(
                new frmCarrinho());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro",
                ex.Message,
                "OK");
        }
        finally
        {
            _ocupado = false;
        }
    }
}