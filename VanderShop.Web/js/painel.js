import { sair } from "./api.js";
import { repositorio, observarDados, normalizar } from "./dados.js";
import { $, dinheiro, escapar, badgeStatus, abrirJanela, mostrarMensagem } from "./comum.js";

let dados;
let carregamento = 0;
let produtoParaExcluir = null;

function desenhar() {
  if (!dados) return;
  const buscaProduto = normalizar($("#pesquisa-produtos").value);
  const produtos = dados.Produtos.map(produto => ({ ...produto,
    Categoria: dados.Categorias.find(c => c.Id === produto.CategoriaId)?.Categoria ?? "Sem categoria"
  })).filter(p => normalizar(`${p.Produto} ${p.Categoria}`).includes(buscaProduto))
    .sort((a, b) => a.Produto.localeCompare(b.Produto, "pt-BR"));
  $("#lista-produtos").innerHTML = produtos.map(p => `<tr>
    <td><span class="nome-produto">${escapar(p.Produto)}</span>${p.Ativo ? "" : '<span class="badge neutro">Inativo</span>'}</td>
    <td>${escapar(p.Categoria)}</td><td class="numero">${dinheiro(p.PrecoCentavos)}</td>
    <td><div class="acoes">
      <button type="button" class="botao secundario pequeno" data-acao="alterar" data-id="${p.Id}" aria-label="Alterar ${escapar(p.Produto)}">Alterar</button>
      <button type="button" class="botao excluir pequeno" data-acao="excluir" data-id="${p.Id}" aria-label="Excluir ${escapar(p.Produto)}">Excluir</button>
    </div></td></tr>`).join("");
  $("#total-produtos").textContent = produtos.length;
  $("#vazio-produtos").hidden = produtos.length > 0;

  const buscaPedido = normalizar($("#pesquisa-pedidos").value);
  const pedidos = dados.Pedidos.map(pedido => ({ ...pedido,
    Cliente: dados.Usuarios.find(u => u.Id === pedido.UsuarioId)?.Nome ?? "Cliente não encontrado"
  })).filter(p => normalizar(`#${p.Id} ${p.Cliente} ${p.Status}`).includes(buscaPedido))
    .sort((a, b) => new Date(b.DataHora) - new Date(a.DataHora));
  $("#lista-pedidos").innerHTML = pedidos.map(p => `<tr>
    <td><strong>#${p.Id}</strong></td><td>${escapar(p.Cliente)}</td><td>${badgeStatus(p.Status)}</td>
    <td class="acoes-coluna"><button type="button" class="botao secundario pequeno" data-id="${p.Id}" aria-label="Ver detalhes do pedido ${p.Id}">Ver detalhes</button></td>
    </tr>`).join("");
  $("#total-pedidos").textContent = pedidos.length;
  $("#vazio-pedidos").hidden = pedidos.length > 0;
}

async function carregar() {
  const versao = ++carregamento;

  try {
    const novos = await repositorio.obterDados();

    if (versao === carregamento) {
      dados = novos;
      desenhar();
    }
  } catch (erro) {
    mostrarMensagem(erro.message, true);
  }
}

$("#pesquisa-produtos").addEventListener("input", desenhar);
$("#pesquisa-pedidos").addEventListener("input", desenhar);
$("#incluir-produto").addEventListener("click", () => abrirJanela("produto.html", "vandershop-produto-novo"));
$("#lista-produtos").addEventListener("click", evento => {
  const botao = evento.target.closest("button[data-acao]");
  if (!botao) return;
  const id = Number(botao.dataset.id);
  if (botao.dataset.acao === "alterar") {
    abrirJanela(`produto.html?id=${id}`, `vandershop-produto-${id}`);
  } else {
    produtoParaExcluir = id;
    $("#nome-exclusao").textContent = dados.Produtos.find(p => p.Id === id)?.Produto ?? "";
    $("#confirmacao-exclusao").showModal();
  }
});
$("#lista-pedidos").addEventListener("click", evento => {
  const botao = evento.target.closest("button[data-id]");
  if (botao) abrirJanela(`pedido.html?id=${botao.dataset.id}`, `vandershop-pedido-${botao.dataset.id}`, 900);
});
$("#cancelar-exclusao").addEventListener("click", () => $("#confirmacao-exclusao").close());
$("#confirmar-exclusao").addEventListener("click", async () => {
  if (produtoParaExcluir === null) return;
  $("#confirmar-exclusao").disabled = true;
  try {
    await repositorio.excluirProduto(produtoParaExcluir);
    $("#confirmacao-exclusao").close();
    produtoParaExcluir = null;
    await carregar();
    mostrarMensagem("Produto excluído.");
  } catch (erro) {
    $("#confirmacao-exclusao").close();
    mostrarMensagem(erro.message, true);
  } finally { $("#confirmar-exclusao").disabled = false; }
});
window.addEventListener("message", evento => {
  if (evento.origin === location.origin && evento.data?.tipo === "vandershop:salvo")
    mostrarMensagem(String(evento.data.mensagem));
});
observarDados(carregar);
carregar();

$("#sair").addEventListener("click", sair);

window.addEventListener("focus", carregar);

setInterval(() => {
  if (!document.hidden) {
    carregar();
  }
}, 15000);