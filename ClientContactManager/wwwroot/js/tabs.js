/* CCM.tabs — tab initialisation helper */

window.CCM = window.CCM || {};

window.CCM.tabs = (function () {

  function init(navSelector) {
    const nav = document.querySelector(navSelector);
    if (!nav) return;

    nav.querySelectorAll('.nav-link').forEach(function (btn) {
      btn.addEventListener('click', function () {
        const targetId = btn.getAttribute('data-bs-target') || btn.getAttribute('data-target');
        if (!targetId) return;

        // Deactivate all
        nav.querySelectorAll('.nav-link').forEach(function (b) {
          b.classList.remove('active');
          b.setAttribute('aria-selected', 'false');
        });

        const tabContent = nav.closest('.app-form-layout') || document;
        tabContent.querySelectorAll('.tab-pane').forEach(function (pane) {
          pane.classList.remove('active', 'show');
        });

        // Activate selected
        btn.classList.add('active');
        btn.setAttribute('aria-selected', 'true');
        const pane = document.querySelector(targetId);
        if (pane) pane.classList.add('active', 'show');
      });
    });
  }

  return { init };

}());
