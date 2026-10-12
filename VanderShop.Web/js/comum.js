export const $ = seletor => document.querySelector(seletor);
export const dinheiro = centavos => (centavos / 100).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
export const dataHora = valor => new Date(valor).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
export const escapar = valor => String(valor ?? "").replace(/[&<>"']/g,
  caractere => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[caractere]);

export function mostrarMensagem(texto, erro = false) {
  const elemento = $("#mensagem");
  elemento.textContent = texto;
  elemento.classList.toggle("erro", erro);
  elemento.setAttribute("role", erro ? "alert" : "status");
  elemento.hidden = false;
}

export function lerCentavos(texto) {
  const valor = texto.trim();
  if (!/^\d+(?:[,.]\d{1,2})?$/.test(valor))
    throw new Error("Informe um preço como 49,90, sem separador de milhar e com até duas casas decimais.");
  const [inteiros, fracao = ""] = valor.replace(",", ".").split(".");
  const centavos = Number(inteiros) * 100 + Number(fracao.padEnd(2, "0"));
  if (!Number.isSafeInteger(centavos)) throw new Error("O preço ultrapassa o valor suportado.");
  return centavos;
}

export function obterId(obrigatorio = true) {
  const texto = new URLSearchParams(location.search).get("id");
  if (texto === null && !obrigatorio) return null;
  const id = Number(texto);
  if (!/^\d+$/.test(texto ?? "") || !Number.isSafeInteger(id) || id <= 0)
    throw new Error("O endereço contém um identificador inválido.");
  return id;
}

export function badgeStatus(status) {
  const cor = status === "Cancelado" ? "vermelho" :
    ["Pedido Confirmado", "Concluído"].includes(status) ? "verde" : "";
  return `<span class="badge ${cor}">${escapar(status)}</span>`;
}

export function abrirJanela(url, nome, largura = 720, altura = 820) {
  const janela = window.open(url, nome, `popup=yes,width=${largura},height=${altura},resizable=yes,scrollbars=yes`);
  if (janela) janela.focus();
  else mostrarMensagem("Permita a abertura de janelas para este site e tente novamente.", true);
  return janela;
}

export function voltar() {
  if (window.opener && !window.opener.closed) window.close();
  else location.assign("index.html");
}

export function concluir(mensagem, extras = {}) {
  if (window.opener && !window.opener.closed) {
    window.opener.postMessage({ tipo: "vandershop:salvo", mensagem, ...extras }, location.origin);
  }
  voltar();
}

document.querySelectorAll("[data-voltar]").forEach(botao => botao.addEventListener("click", voltar));
