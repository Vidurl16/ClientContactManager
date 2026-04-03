document.addEventListener('DOMContentLoaded', function () {
  const cfg = window.clientsConfig;
  if (!cfg) return;

  const v   = CCM.validation;
  const ax  = CCM.ajax;

  const form = document.getElementById('client-form');
  if (!form) return;

  // ── Form submit ─────────────────────────────────────────────────────────────

  form.addEventListener('submit', async function (e) {
    e.preventDefault();

    const nameInput = document.getElementById('clientName');
    if (!v.validateRequired(nameInput, 'Name is required.')) return;

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

  // ── Inline name validation ──────────────────────────────────────────────────

  const nameInput = document.getElementById('clientName');
  if (nameInput) {
    nameInput.addEventListener('input', function () {
      v.validateRequired(this, 'Name is required.');
    });
  }

  // ── Link contact ────────────────────────────────────────────────────────────

  const linkBtn = document.getElementById('link-btn');
  if (linkBtn) {
    linkBtn.addEventListener('click', async function () {
      const select = document.getElementById('contact-select');
      if (!select.value) return;

      linkBtn.disabled = true;

      const fd = new FormData();
      fd.append('clientId',  cfg.clientId);
      fd.append('contactId', select.value);
      fd.append('__RequestVerificationToken', v.getToken());

      try {
        const data = await ax.post(cfg.linkUrl, fd);

        if (data.success) {
          const c        = data.contact;
          const fullName = `${c.surname} ${c.name}`;
          addContactRow(c.id, fullName, c.email);

          const opt = select.querySelector(`option[value="${c.id}"]`);
          if (opt) opt.remove();
          select.value = '';

          toggleEmptyState();
        } else {
          v.showAlert('danger', data.message || 'Could not link contact.');
        }
      } catch {
        v.showAlert('danger', 'A network error occurred.');
      } finally {
        linkBtn.disabled = false;
      }
    });
  }

  // ── Unlink contact (delegated) ──────────────────────────────────────────────

  const tbody = document.getElementById('contacts-tbody');
  if (tbody) {
    tbody.addEventListener('click', async function (e) {
      const btn = e.target.closest('.unlink-btn');
      if (!btn) return;

      const contactId = btn.dataset.contactId;
      btn.disabled = true;

      const fd = new FormData();
      fd.append('clientId',  cfg.clientId);
      fd.append('contactId', contactId);
      fd.append('__RequestVerificationToken', v.getToken());

      try {
        const data = await ax.post(cfg.unlinkUrl, fd);

        if (data.success) {
          tbody.querySelector(`tr[data-contact-id="${contactId}"]`)?.remove();

          const c = data.contact;
          addToDropdown(c.id, `${c.surname} ${c.name}`);

          toggleEmptyState();
        } else {
          v.showAlert('danger', data.message || 'Could not unlink contact.');
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
    const tb    = document.getElementById('contacts-tbody');
    const table = document.getElementById('contacts-table');
    const msg   = document.getElementById('no-contacts-msg');
    if (!tb || !table || !msg) return;

    const hasRows = tb.querySelectorAll('tr').length > 0;
    table.classList.toggle('d-none', !hasRows);
    msg.classList.toggle('d-none', hasRows);
  }

  function addContactRow(id, fullName, email) {
    const tb = document.getElementById('contacts-tbody');
    if (!tb) return;

    const row = document.createElement('tr');
    row.dataset.contactId = id;
    row.innerHTML = `
      <td>${v.escHtml(fullName)}</td>
      <td>${v.escHtml(email)}</td>
      <td class="col-end">
        <button class="btn btn-sm btn-outline-danger unlink-btn"
                data-contact-id="${id}"
                data-contact-name="${v.escHtml(fullName)}"
                type="button">Unlink</button>
      </td>`;

    const rows = Array.from(tb.querySelectorAll('tr'));
    const insertBefore = rows.find(r =>
      r.cells[0].textContent.trim().localeCompare(fullName) > 0
    );
    insertBefore ? tb.insertBefore(row, insertBefore) : tb.appendChild(row);
  }

  function addToDropdown(id, fullName) {
    const select = document.getElementById('contact-select');
    if (!select) return;

    const opt = document.createElement('option');
    opt.value = id;
    opt.textContent = fullName;

    const opts = Array.from(select.options).slice(1);
    const insertBefore = opts.find(o => o.textContent.localeCompare(fullName) > 0);
    insertBefore ? select.insertBefore(opt, insertBefore) : select.appendChild(opt);
  }
});
