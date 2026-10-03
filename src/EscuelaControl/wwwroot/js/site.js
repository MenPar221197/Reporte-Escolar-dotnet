// Evita dobles envíos accidentales; las validaciones definitivas viven en el servidor.
document.querySelectorAll('form[method="post"]').forEach(form => {
  form.addEventListener('submit', () => {
    if (!form.checkValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    if (button) { button.disabled = true; button.dataset.originalText = button.textContent; button.textContent = 'Procesando…'; }
  });
});
window.addEventListener('pageshow', () => {
  document.querySelectorAll('button[data-original-text]').forEach(button => {
    button.disabled = false; button.textContent = button.dataset.originalText;
  });
});
