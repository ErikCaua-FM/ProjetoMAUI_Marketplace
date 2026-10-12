using VanderShop.Models;

namespace VanderShop.Views;

public partial class frmLogin : ContentPage
{
    private bool _processando;
    private bool _abrindoCadastro;

    public frmLogin()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        return _processando || base.OnBackButtonPressed();
    }

    private async void Entrar_Clicked(
        object sender,
        EventArgs e)
    {
        if (_processando || _abrindoCadastro)
            return;

        _processando = true;

        formulario.IsEnabled = false;
        btnEntrar.Text = "Entrando...";

        NavigationPage.SetHasBackButton(this, false);

        try
        {
            await App.EntrarAsync(
                txtEmail.Text ?? "",
                txtSenha.Text ?? "");

            txtSenha.Text = "";

            await Navigation.PopToRootAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível entrar",
                ex.Message,
                "OK");
        }
        finally
        {
            _processando = false;

            formulario.IsEnabled = true;
            btnEntrar.Text = "Entrar";

            NavigationPage.SetHasBackButton(this, true);
        }
    }

    private async void CriarConta_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (_processando || _abrindoCadastro)
            return;

        _abrindoCadastro = true;

        try
        {
            await Navigation.PushAsync(new frmCadastro());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível abrir o cadastro",
                ex.Message,
                "OK");
        }
        finally
        {
            _abrindoCadastro = false;
        }
    }
}