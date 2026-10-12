using System.Security.Claims;
using System.Text.RegularExpressions;
using VanderShop.Api.Models;
using VanderShop.Api.Requests;

namespace VanderShop.Api.Infrastructure;

public static class Regras
{
    public const long MaxCentavos = 9007199254740991;

    public const string Retirada = "Retirar na loja";
    public const string Entrega = "Realizar uma entrega";

    public static readonly string[] Pagamentos =
    [
        "Pix",
        "Cartão de crédito",
        "Cartão de débito",
        "Boleto bancário"
    ];

    public static readonly string[] Status =
    [
        "Pedido Realizado",
        "Pedido Confirmado",
        "Em Preparação",
        "Pronto para Retirada",
        "Pedido Enviado",
        "Concluído",
        "Cancelado"
    ];

    public static int UsuarioId(ClaimsPrincipal user)
    {
        var identificador = user.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (int.TryParse(identificador, out var id) && id > 0)
            return id;

        throw new ApiException(
            401,
            "Entre novamente na sua conta."
        );
    }

    public static void Exigir(
        bool condicao,
        string mensagem,
        int status = 400)
    {
        if (!condicao)
            throw new ApiException(status, mensagem);
    }

    public static string Digitos(string? valor)
    {
        return Regex.Replace(valor ?? "", "[^0-9]", "");
    }

    public static long Valor(decimal valor)
    {
        Exigir(
            valor >= 0 && valor <= MaxCentavos,
            "O valor ultrapassa o limite monetário suportado."
        );

        return (long)valor;
    }

    public static void Endereco(PedidoRequest pedido)
    {
        Exigir(
            Pagamentos.Contains(pedido.FormaPagamento),
            "Selecione uma forma de pagamento válida."
        );

        Exigir(
            pedido.FormaRecebimento is Retirada or Entrega,
            "Selecione a forma de recebimento."
        );

        if (pedido.FormaRecebimento == Retirada)
        {
            pedido.CEP = "";
            pedido.Rua = "";
            pedido.Bairro = "";
            pedido.Numero = "";
            pedido.Complemento = "";
            return;
        }

        var cep = Digitos(pedido.CEP);

        pedido.Rua = (pedido.Rua ?? "").Trim();
        pedido.Bairro = (pedido.Bairro ?? "").Trim();
        pedido.Numero = (pedido.Numero ?? "").Trim();
        pedido.Complemento = (pedido.Complemento ?? "").Trim();

        Exigir(
            cep.Length == 8 &&
            pedido.Rua.Length > 0 &&
            pedido.Bairro.Length > 0 &&
            pedido.Numero.Length > 0,
            "Para entrega, informe CEP, rua, bairro e número."
        );

        pedido.CEP = cep.Insert(5, "-");
    }

    public static string[] Proximos(Pedidos pedido)
    {
        return pedido.Status switch
        {
            "Pedido Realizado" =>
                ["Pedido Confirmado", "Cancelado"],

            "Pedido Confirmado" =>
                ["Em Preparação", "Cancelado"],

            "Em Preparação" =>
            [
                pedido.FormaRecebimento == Retirada
                    ? "Pronto para Retirada"
                    : "Pedido Enviado",

                "Cancelado"
            ],

            "Pronto para Retirada" =>
                ["Concluído", "Cancelado"],

            "Pedido Enviado" =>
                ["Concluído"],

            _ => []
        };
    }
}