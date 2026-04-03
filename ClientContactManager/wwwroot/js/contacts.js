document.addEventListener('DOMContentLoaded', function () {
  const cfg = window.contactsConfig;
  if (!cfg) return;

  const v  = CCM.validation;
  const ax = CCM.ajax;

  const form = document.getElementById('contact-form');
  if (!form) return;

  // ── Form submit ─────────────────────────────────────────────────────────────

  form.addEventListener('submit', async function (e) {
    e.preventDefault();

    const nameInput    = document.getElementById('contactName');
    const surnameInput = document.getElementById('contactSurname');
    const emailInput   = document.getElementById('contactEmail');

    const nameValid    = v.validateRequired(nameInput,    'Name is required.');
    const surnameValid = v.validateRequired(surnameInput, 'Surname is required.');
    const emailValid   = v.validateEmail(emailInput);

    if (!nameValid || !surnameValid || !emailValid) return;

    const btn = document.getElementById('save-btn');
    btn.disabled = true;
    btn.textContent = 'Saving…';

    const url = cfg.isNew ? cfg.createUrl : cfg.editUrl;

    try {
      const data = await ax.post(url, new FormData(form));

      if (data.success) {
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
        } else {
          v.showAlert('success', data.message);
        }
      } else {
        v.showAlert('danger', data.message || 'An error occurred.');
      }
    } catch {
      v.showAlert('danger', 'A network error occurred.');
    } finally {
      btn.disabled = false;
      btn.textContent = 'Save';
    }
  });

  // ── Inline field validation ─────────────────────────────────────────────────

  const nameInput = document.getElementById('contactName');
  if (nameInput) {
    nameInput.addEventListener('input', function () {
      v.validateRequired(this, 'Name is required.');
    });
  }

  const surnameInput = document.getElementById('contactSurname');
  if (surnameInput) {
    surnameInput.addEventListener('input', function () {
      v.validateRequired(this, 'Surname is required.');
    });
  }

  const emailInput = document.getElementById('contactEmail');
  if (emailInput) {
    emailInput.addEventListener('input', function () { v.validateEmail(this); });
  }

  // ── Link client ─────────────────────────────────────────────────────────────

  const linkBtn = document.getElementById('link-btn');
  if (linkBtn) {
    linkBtn.addEventListener('click', async function () {
      const select = document.getElementById('client-select');
      if (!select.value) return;

      linkBtn.disabled = true;

      const fd = new FormData();
      fd.append('contactId', cfg.contactId);
      fd.append('clientId',  select.value);
      fd.append('__RequestVerificationToken', v.getToken());

      try {
        const data = await ax.post(cfg.linkUrl, fd);

        if (data.success) {
          const c = data.client;
          addClientRow(c.id, c.name, c.clientCode);

          const opt = select.querySelector(`option[value="${c.id}"]`);
          if (opt) opt.remove();
          select.value = '';

          toggleEmptyState();
        } else {
          v.showAlert('danger', data.message || 'Could not link client.');
        }
      } catch {
        v.showAlert('danger', 'A network error occurred.');
      } finally {
        linkBtn.disabled = false;
      }
    });
  }

  // ── Unlink client (delegated) ───────────────────────────────────────────────

  const tbody = document.getElementById('clients-tbody');
  if (tbody) {
    tbody.addEventListener('click', async function (e) {
      const btn = e.target.closest('.unlink-btn');
      if (!btn) return;

      const clientId = btn.dataset.clientId;
      btn.disabled = true;

      const fd = new FormData();
      fd.append('contactId', cfg.contactId);
      fd.append('clientId',  clientId);
      fd.append('__RequestVerificationToken', v.getToken());

      try {
        const data = await ax.post(cfg.unlinkUrl, fd);

        if (data.success) {
          tbody.querySelector(`tr[data-client-id="${clientId}"]`)?.remove();

          const c = data.client;
          addToDropdown(c.id, c.name);

          toggleEmptyState();
        } else {
          v.showAlert('danger', data.message || 'Could not unlink client.');
          btn.disabled = false;
        }
      } catch {
        v.showAlert('danger', 'A network error occurred.');
        btn.disabled = false;
      }
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  function toggleEmptyState() {
    const tb    = document.getElementById('clients-tbody');
    const table = document.getElementById('clients-table');
    const msg   = document.getElementById('no-clients-msg');
    if (!tb || !table || !msg) return;

    const hasRows = tb.querySelectorAll('tr').length > 0;
    table.classList.toggle('d-none', !hasRows);
    msg.classList.toggle('d-none', hasRows);
  }

  function addClientRow(id, name, clientCode) {
    const tb = document.getElementById('clients-tbody');
    if (!tb) return;

    const row = document.createElement('tr');
    row.dataset.clientId = id;
    row.innerHTML = `
      <td>${v.escHtml(name)}</td>
      <td>${v.escHtml(clientCode)}</td>
      <td class="col-end">
        <button class="btn btn-sm btn-outline-danger unlink-btn"
                data-client-id="${id}"
                type="button">Unlink</button>
      </td>`;

    const rows = Array.from(tb.querySelectorAll('tr'));
    const insertBefore = rows.find(r =>
      r.cells[0].textContent.trim().localeCompare(name) > 0
    );
    insertBefore ? tb.insertBefore(row, insertBefore) : tb.appendChild(row);
  }

  function addToDropdown(id, name) {
    const select = document.getElementById('client-select');
    if (!select) return;

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = name;

    const opts = Array.from(select.options).slice(1);
    const insertBefore = opts.find(o => o.textContent.localeCompare(name) > 0);
    insertBefore ? select.insertBefore(opt, insertBefore) : select.appendChild(opt);
  }
});
