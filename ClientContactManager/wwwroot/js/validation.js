/* CCM.validation — shared form validation helpers */

window.CCM = window.CCM || {};

window.CCM.validation = (function () {

  function validateRequired(input, message) {
    const valid = input.value.trim().length > 0;
    _setValidity(input, valid);
    const feedback = input.nextElementSibling;
    if (feedback && feedback.classList.contains('invalid-feedback')) {
      feedback.textContent = message;
    }
    return valid;
  }

  function validateEmail(input) {
    const val = input.value.trim();
    const feedback = document.getElementById('email-feedback');

    if (!val) {
      _setValidity(input, false);
      if (feedback) feedback.textContent = 'Email is required.';
      return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(val)) {
      _setValidity(input, false);
      if (feedback) feedback.textContent = 'Email is not valid.';
      return false;
    }

    _setValidity(input, true);
    return true;
  }

  function showAlert(type, message) {
    const container = document.getElementById('alert-container');
    if (!container) return;

    const alert = document.createElement('div');
    alert.className = `app-alert app-alert--${type}`;
    alert.innerHTML = `
      <span>${escHtml(message)}</span>
      <button class="app-alert__close" aria-label="Close">&times;</button>`;

    alert.querySelector('.app-alert__close').addEventListener('click', () => alert.remove());

    container.innerHTML = '';
    container.appendChild(alert);
    container.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }

  function getToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
  }

  function escHtml(str) {
    const div = document.createElement('div');
    div.textContent = String(str);
    return div.innerHTML;
  }

  function _setValidity(input, valid) {
    input.classList.toggle('is-invalid', !valid);
    input.classList.toggle('is-valid', valid);
  }

  return { validateRequired, validateEmail, showAlert, getToken, escHtml };

}());
