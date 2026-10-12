import { API_URL } from "./config.js";

const SESSAO = "vandershop.admin.sessao.v1";
const LOGOUT = "vandershop.admin.logout.v1";

function sessao() {
  try {
    return JSON.parse(
      sessionStorage.getItem(SESSAO) ?? "null"
    );
  } catch {
    return null;
  }
}

export function conectado() {
  const dados = sessao();

  return Boolean(
    dados?.token &&
    dados.expira > Date.now()
  );
}

export function sair() {
  sessionStorage.removeItem(SESSAO);

  localStorage.setItem(
    LOGOUT,
    crypto.randomUUID()
  );

  location.replace("login.html");
}

window.addEventListener("storage", evento => {
  if (evento.key === LOGOUT) {
    sessionStorage.removeItem(SESSAO);
    location.replace("login.html");
  }
});

export function exigirLogin() {
  if (!conectado()) {
    location.replace("login.html");
  }
}

export async function requisicao(
  caminho,
  metodo = "GET",
  corpo,
  autenticada = true
) {
  if (autenticada && !conectado()) {
    exigirLogin();
    throw new Error("Entre na administração.");
  }

  const headers = {
    Accept: "application/json"
  };

  if (corpo !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (autenticada) {
    headers.Authorization = `Bearer ${sessao().token}`;
  }

  let resposta;

  try {
    resposta = await fetch(`${API_URL}${caminho}`, {
      method: metodo,
      headers,
      body: corpo === undefined
        ? undefined
        : JSON.stringify(corpo),
      signal: AbortSignal.timeout(20000)
    });
  } catch {
    throw new Error(
      "Não foi possível acessar a API. " +
      "Verifique se ela está executando."
    );
  }

  const texto = await resposta.text();

  let dados;

  try {
    dados = texto ? JSON.parse(texto) : null;
  } catch {
    dados = null;
  }

  if (!resposta.ok) {
    if (autenticada && resposta.status === 401) {
      sair();
    }

    const errosDosCampos = Object
      .values(dados?.Erros ?? {})
      .flat()
      .join(" ");

    throw new Error(
      errosDosCampos ||
      dados?.Mensagem ||
      dados?.mensagem ||
      dados?.detail ||
      (
        resposta.status === 403
          ? "Seu acesso não permite esta operação."
          : `Erro ${resposta.status} ao acessar a API.`
      )
    );
  }

  return dados;
}

export async function entrar(email, senha) {
  const resposta = await requisicao(
    "/auth/admin/login",
    "POST",
    {
      Email: email.trim(),
      Senha: senha
    },
    false
  );

  sessionStorage.setItem(
    SESSAO,
    JSON.stringify({
      token: resposta.AccessToken,
      expira: Date.now() + resposta.ExpiresIn * 1000
    })
  );
}