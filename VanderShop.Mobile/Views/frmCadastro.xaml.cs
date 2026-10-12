using VanderShop.Models;

namespace VanderShop.Views;

public partial class frmCadastro : ContentPage
{
    private bool _processando;
    private bool _voltando;

    public frmCadastro()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        return _processando || base.OnBackButtonPressed();
    }

    private async void Cadastrar_Clicked(
        object sender,
        EventArgs e)
    {
        if (_processando || _voltando)
            return;

        _processando = true;

        formulario.IsEnabled = false;
        btnCadastrar.Text = "Criando conta...";

        NavigationPage.SetHasBackButton(this, false);

        bool contaCriada = false;

        try
        {
            string senha = txtSenha.Text ?? "";

            if (senha != (txtConfirmacao.Text ?? ""))
            {
                throw new InvalidOperationException(
                    "A confirmação da senha não confere.");
            }

            var usuario =
                await App.Api.CadastrarAsync(
                    new Usuarios
                    {
                        Nome = txtNome.Text ?? "",
                        Documento = txtDocumento.Text ?? "",
                        Telefone = txtTelefone.Text ?? "",
                        Email = txtEmail.Text ?? ""
                    },
                    senha);

            contaCriada = true;

            await App.EntrarAsync(usuario.Email, senha);

            txtSenha.Text = "";
            txtConfirmacao.Text = "";

            await Navigation.PopToRootAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                contaCriada ? "Conta criada" : "Não foi possível criar a conta",
                contaCriada
                    ? "Seu cadastro foi salvo. Faça login para continuar.\n" + ex.Message
                    : ex.Message,
                "OK");

            if (contaCriada)
                await Navigation.PopAsync();
        }
        finally
        {
            _processando = false;

            formulario.IsEnabled = true;
            btnCadastrar.Text = "Criar minha conta";

            NavigationPage.SetHasBackButton(this, true);
        }
    }

    private async void VoltarLogin_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (_processando || _voltando)
            return;

        _voltando = true;

        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Não foi possível voltar",
                ex.Message,
                "OK");
        }
        finally
        {
            _voltando = false;
        }
    }
}