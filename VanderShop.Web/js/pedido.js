import {
  repositorio,
  STATUS,
  proximosStatus
} from "./dados.js";
import { $, obterId, dinheiro, dataHora, escapar, mostrarMensagem } from "./comum.js";

let pedido = null;
let salvando = false;

function atualizarOpcoes() {
  const permitidos = proximosStatus(pedido);

  for (const opcao of $("#status").options) {
    opcao.disabled =
      opcao.value !== pedido.Status &&
      !permitidos.includes(opcao.value);
  }

  $("#status").value = pedido.Status;
}

async function iniciar() {
  try {
    const id = obterId();
    const dados = await repositorio.obterDados();
    pedido = dados.Pedidos.find(p => p.Id === id);
    if (!pedido) throw new Error("Pedido não encontrado.");
    const usuario = dados.Usuarios.find(u => u.Id === pedido.UsuarioId);
    const itens = dados.ItensPedido.filter(i => i.PedidoId === id);
    $("#titulo-pagina").textContent = `Pedido #${id}`;
    document.title = `Pedido #${id} | VanderShop`;
    $("#data-pedido").textContent = `Realizado em ${dataHora(pedido.DataHora)}`;
    $("#cliente").textContent = usuario?.Nome ?? "Cliente não encontrado";
    $("#contato").textContent = usuario?.Email ?? "";
    $("#lista-itens").innerHTML = itens.map(item => `<tr>
      <td>${escapar(item.NomeProduto)}</td><td class="numero">${item.Quantidade}</td>
      <td class="numero">${dinheiro(item.PrecoUnitarioCentavos)}</td>
      <td class="numero"><strong>${dinheiro(item.SubtotalCentavos)}</strong></td></tr>`).join("");
    $("#recebimento").textContent = pedido.FormaRecebimento;
    $("#endereco").textContent = pedido.FormaRecebimento === "Realizar uma entrega"
      ? `${pedido.Rua}, ${pedido.Numero}${pedido.Complemento ? ` — ${pedido.Complemento}` : ""}\n${pedido.Bairro} — CEP: ${pedido.CEP}`
      : "Retirada na loja.";
    $("#pagamento").textContent = pedido.FormaPagamento;
    $("#total-pedido").textContent = dinheiro(pedido.TotalCentavos);
    STATUS.forEach(status => $("#status").add(new Option(status, status)));
    atualizarOpcoes();
    $("#status").value = pedido.Status;
    $("#conteudo-pedido").hidden = false;
    $("#campos").disabled = false;
  } catch (erro) { mostrarMensagem(erro.message, true); }
}

$("#status").addEventListener("change", () => {
  $("#salvar").hidden = !pedido || $("#status").value === pedido.Status;
});
$("#form-status").addEventListener("submit", async evento => {
  evento.preventDefault();
  if (!pedido || salvando || $("#status").value === pedido.Status) return;
  salvando = true;
  $("#campos").disabled = true;
  try {
    pedido = await repositorio.alterarStatus(pedido.Id, $("#status").value, pedido.Status);
    atualizarOpcoes();
    $("#salvar").hidden = true;
    mostrarMensagem("Status do pedido atualizado.");
  } catch (erro) { mostrarMensagem(erro.message, true); }
  finally { salvando = false; $("#campos").disabled = false; }
});
iniciar();
