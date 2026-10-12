import { repositorio, observarDados } from "./dados.js";
import { $, obterId, lerCentavos, mostrarMensagem, abrirJanela, concluir } from "./comum.js";

let id = null;
let janelaCategoria = null;
let salvando = false;
let pronto = false;
let categoriaEscolhida = "";
let versaoCategorias = 0;

async function carregarCategorias(selecionada) {
  if (selecionada !== undefined) {
    categoriaEscolhida = String(selecionada);
  }

  const versao = ++versaoCategorias;

  const dados = await repositorio.obterDados();

  if (versao !== versaoCategorias) {
    return;
  }

  const select = $("#categoria");

  select.replaceChildren(
    new Option("Selecione uma categoria", "")
  );

  dados.Categorias
    .sort((a, b) =>
      a.Categoria.localeCompare(
        b.Categoria,
        "pt-BR"
      )
    )
    .forEach(categoria => {
      select.add(
        new Option(
          categoria.Categoria,
          String(categoria.Id)
        )
      );
    });

  select.value = categoriaEscolhida;
}

$("#categoria").addEventListener("change", () => {
  categoriaEscolhida = $("#categoria").value;
});

async function iniciar() {
  try {
    id = obterId(false);
    const dados = await repositorio.obterDados();
    const produto = id === null ? null : dados.Produtos.find(p => p.Id === id);
    if (id !== null && !produto) throw new Error("Produto não encontrado.");
    await carregarCategorias(produto?.CategoriaId ?? "");
    if (produto) {
      $("#titulo-pagina").textContent = "Alterar produto";
      document.title = "Alterar produto | VanderShop";
      $("#salvar").textContent = "Salvar";
      $("#nome").value = produto.Produto;
      $("#descricao").value = produto.Descricao;
      $("#preco").value = (produto.PrecoCentavos / 100).toFixed(2).replace(".", ",");
      $("#ativo").checked = produto.Ativo;
    }
    pronto = true;
    $("#campos").disabled = false;
  } catch (erro) { mostrarMensagem(erro.message, true); }
}

$("#nova-categoria").addEventListener("click", () => {
  janelaCategoria = abrirJanela("categoria.html", `vandershop-categoria-${id ?? "novo"}`, 560, 520);
});
window.addEventListener("message", async evento => {
  if (evento.origin !== location.origin || evento.source !== janelaCategoria ||
      evento.data?.tipo !== "vandershop:salvo" || !evento.data.categoriaId) return;
  try {
    await carregarCategorias(evento.data.categoriaId);
    mostrarMensagem("Categoria cadastrada e selecionada.");
  } catch (erro) { mostrarMensagem(erro.message, true); }
});
observarDados(async () => {
  if (!pronto) return;
  try { await carregarCategorias(); }
  catch (erro) { mostrarMensagem(erro.message, true); }
});
$("#form-produto").addEventListener("submit", async evento => {
  evento.preventDefault();
  if (!pronto || salvando) return;
  salvando = true;
  $("#campos").disabled = true;
  try {
    await repositorio.salvarProduto({
      Produto: $("#nome").value,
      Descricao: $("#descricao").value,
      CategoriaId: $("#categoria").value,
      PrecoCentavos: lerCentavos($("#preco").value),
      Ativo: $("#ativo").checked
    }, id);
    concluir(id === null ? "Produto cadastrado." : "Produto atualizado.");
  } catch (erro) { mostrarMensagem(erro.message, true); }
  finally { salvando = false; $("#campos").disabled = false; }
});
iniciar();
