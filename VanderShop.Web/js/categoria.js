import { repositorio } from "./dados.js";
import { $, mostrarMensagem, concluir } from "./comum.js";

let salvando = false;
$("#form-categoria").addEventListener("submit", async evento => {
  evento.preventDefault();
  if (salvando) return;
  salvando = true;
  $("#campos").disabled = true;
  try {
    const categoria = await repositorio.salvarCategoria($("#nome").value);
    concluir("Categoria cadastrada.", { categoriaId: categoria.Id });
  } catch (erro) { mostrarMensagem(erro.message, true); }
  finally { salvando = false; $("#campos").disabled = false; }
});
