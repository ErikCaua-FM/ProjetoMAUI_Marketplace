import { entrar, conectado } from "./api.js";
import { $, mostrarMensagem } from "./comum.js";

if (conectado()) {
  location.replace("index.html");
}

$("#form-login").addEventListener(
  "submit",
  async evento => {
    evento.preventDefault();

    $("#campos").disabled = true;

    try {
      await entrar(
        $("#email").value,
        $("#senha").value
      );

      location.replace("index.html");
    } catch (erro) {
      mostrarMensagem(erro.message, true);
    } finally {
      $("#campos").disabled = false;
    }
  }
);