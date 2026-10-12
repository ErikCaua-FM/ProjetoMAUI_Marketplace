import { requisicao, exigirLogin } from "./api.js";

exigirLogin();

localStorage.removeItem("vandershop.web.demo.v1");

export const CHAVE = "vandershop.web.atualizacao.v2";
export const EVENTO = "vandershop:dados-alterados";

export const STATUS = Object.freeze([
  "Pedido Realizado",
  "Pedido Confirmado",
  "Em Preparação",
  "Pronto para Retirada",
  "Pedido Enviado",
  "Concluído",
  "Cancelado"
]);

export function normalizar(valor) {
  return String(valor ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function notificar() {
  localStorage.setItem(
    CHAVE,
    crypto.randomUUID()
  );

  window.dispatchEvent(new Event(EVENTO));
}

async function alterar(caminho, metodo, corpo) {
  const resultado = await requisicao(
    caminho,
    metodo,
    corpo
  );

  notificar();

  return resultado;
}

export const repositorio = {
  obterDados: () => requisicao("/admin/painel"),

  salvarCategoria: nome => alterar(
    "/categorias",
    "POST",
    {
      Categoria: nome.trim()
    }
  ),

  salvarProduto: (valores, id = null) => alterar(
    id === null
      ? "/produtos"
      : `/produtos/${id}`,
    id === null ? "POST" : "PUT",
    {
      ...valores,
      CategoriaId: Number(valores.CategoriaId)
    }
  ),

  excluirProduto: id => alterar(
    `/produtos/${id}`,
    "DELETE"
  ),

  alterarStatus: (id, status, statusAnterior) => alterar(
    `/admin/pedidos/${id}/status`,
    "PUT",
    {
      Status: status,
      StatusAnterior: statusAnterior
    }
  )
};

export function observarDados(callback) {
  window.addEventListener(EVENTO, callback);

  window.addEventListener("storage", evento => {
    if (evento.key === CHAVE) {
      callback();
    }
  });
}

export function proximosStatus(pedido) {
  const possibilidades = {
    "Pedido Realizado": [
      "Pedido Confirmado",
      "Cancelado"
    ],

    "Pedido Confirmado": [
      "Em Preparação",
      "Cancelado"
    ],

    "Em Preparação": [
      pedido.FormaRecebimento === "Retirar na loja"
        ? "Pronto para Retirada"
        : "Pedido Enviado",

      "Cancelado"
    ],

    "Pronto para Retirada": [
      "Concluído",
      "Cancelado"
    ],

    "Pedido Enviado": [
      "Concluído"
    ]
  };

  return possibilidades[pedido.Status] ?? [];
}