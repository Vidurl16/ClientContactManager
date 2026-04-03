/* CCM.ajax — shared fetch helpers */

window.CCM = window.CCM || {};

window.CCM.ajax = (function () {

  async function post(url, formData) {
    const res = await fetch(url, { method: 'POST', body: formData });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  }

  return { post };

}());
