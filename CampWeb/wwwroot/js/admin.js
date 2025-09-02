// wwwroot/js/admin.js
// Jednotné utilitky pro admin rozhraní (bez závislostí; Bootstrap/Chart/DataTables volitelné)

(() => {
    'use strict';

    // ====== Pomocné funkce =====================================================

    const COLORS = {
        success: '#198754',
        danger:  '#dc3545',
        warning: '#ffc107',
        info:    '#0d6efd',
        secondary: '#6c757d'
    };

    const ICONS = {
        success: 'fas fa-check-circle',
        danger:  'fas fa-exclamation-circle',
        warning: 'fas fa-exclamation-triangle',
        info:    'fas fa-info-circle',
        secondary: 'fas fa-info-circle'
    };

    function colorFor(type) {
        const t = (type || 'info').toLowerCase();
        return COLORS[t] || COLORS.info;
    }

    function variantFor(type) {
        const t = (type || 'info').toLowerCase();
        switch (t) {
            case 'success': return 'success';
            case 'error':   return 'danger';
            case 'warning': return 'warning';
            case 'info':    return 'info';
            default:        return 'secondary';
        }
    }

    function iconFor(type) {
        const t = variantFor(type);
        return ICONS[t] || ICONS.info;
    }

    function ensureContainer(id, styles) {
        let el = document.getElementById(id);
        if (!el) {
            el = document.createElement('div');
            el.id = id;
            Object.assign(el.style, styles || {});
            document.body.appendChild(el);
        }
        return el;
    }

    // Debounce bez závislostí
    function debounce(fn, wait = 300) {
        let t;
        return (...args) => {
            clearTimeout(t);
            t = setTimeout(() => fn(...args), wait);
        };
    }

    // ====== Toasty =============================================================

    function showToast(title, message, type = 'info', timeoutMs = 3500) {
        const container = ensureContainer('toast-container', {
            position: 'fixed',
            top: '1rem',
            right: '1rem',
            zIndex: '1080',
            display: 'flex',
            flexDirection: 'column',
            gap: '.5rem'
        });

        const toast = document.createElement('div');
        const border = colorFor(type);
        const bsVariant = variantFor(type);
        const icon = iconFor(type);

        // Pokud je Bootstrap CSS, použijeme jeho „alert“ třídy; jinak vše pokryjeme inline styly
        toast.className = `alert alert-${bsVariant} alert-dismissible`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        Object.assign(toast.style, {
            margin: '0',
            background: '#fff',
            border: '1px solid #e9ecef',
            borderLeft: `4px solid ${border}`,
            borderRadius: '8px',
            boxShadow: '0 0.25rem 1rem rgba(0,0,0,.08)',
            padding: '.75rem 1rem',
            transition: 'opacity .2s ease, transform .2s ease'
        });

        toast.innerHTML = `
      <div class="d-flex align-items-start" style="gap:.75rem">
        <i class="${icon}" style="color:${border}"></i>
        <div class="flex-grow-1">
          <div class="fw-bold">${title || 'Info'}</div>
          <div>${message || ''}</div>
        </div>
        <button type="button" class="btn-close" aria-label="Zavřít" style="margin-left:.5rem"></button>
      </div>`;

        const closeBtn = toast.querySelector('.btn-close');
        const remove = () => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-4px)';
            setTimeout(() => toast.remove(), 180);
        };
        closeBtn.addEventListener('click', remove);

        container.appendChild(toast);
        // malé „přisunutí“
        requestAnimationFrame(() => { toast.style.opacity = '1'; toast.style.transform = 'translateY(0)'; });
        setTimeout(remove, timeoutMs);
    }

    // ====== Modal utilities (Bootstrap fallback) ===============================

    function showModal(id) {
        const el = document.getElementById(id);
        if (!el) return;

        if (window.bootstrap && bootstrap.Modal) {
            const instance = bootstrap.Modal.getOrCreateInstance(el);
            instance.show();
        } else {
            // fallback bez Bootstrapu
            Object.assign(el.style, {
                display: 'block',
                position: 'fixed',
                inset: '0',
                background: 'rgba(0,0,0,.5)',
                zIndex: '1050'
            });
        }
    }

    function hideModal(id) {
        const el = document.getElementById(id);
        if (!el) return;

        if (window.bootstrap && bootstrap.Modal) {
            const instance = bootstrap.Modal.getInstance(el);
            instance?.hide();
        } else {
            el.style.display = 'none';
        }
    }

    // Potvrzovací dialog (vanilla, bez knihoven). Vrací Promise<boolean>
    function adminConfirm(title, message, confirmText = 'Ano', cancelText = 'Zrušit') {
        return new Promise(resolve => {
            let m = document.getElementById('admin-confirm');
            if (m) m.remove();

            m = document.createElement('div');
            m.id = 'admin-confirm';
            m.innerHTML = `
        <div style="position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:1060;display:flex;align-items:center;justify-content:center;">
          <div style="background:#fff;border-radius:10px;max-width:520px;width:92%;box-shadow:0 20px 60px rgba(0,0,0,.25);">
            <div style="padding:16px 18px;border-bottom:1px solid #eee;font-weight:600;">${title || 'Potvrzení'}</div>
            <div style="padding:16px 18px;">${message || ''}</div>
            <div style="padding:12px 18px;border-top:1px solid #eee;display:flex;gap:8px;justify-content:flex-end;">
              <button id="ac-cancel" class="btn btn-light">${cancelText}</button>
              <button id="ac-ok" class="btn btn-danger">${confirmText}</button>
            </div>
          </div>
        </div>`;
            document.body.appendChild(m);
            m.querySelector('#ac-cancel').onclick = () => { m.remove(); resolve(false); };
            m.querySelector('#ac-ok').onclick     = () => { m.remove(); resolve(true);  };
        });
    }

    // ====== Download / CSV =====================================================

    function downloadFile(url, filename) {
        try {
            const a = document.createElement('a');
            a.href = url;
            if (filename) a.download = filename;
            a.target = '_blank';
            document.body.appendChild(a);
            a.click();
            a.remove();
        } catch (e) { console.error('downloadFile:', e); }
    }

    function downloadCsv(filename, csvContent) {
        try {
            const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url; a.download = filename || 'export.csv';
            document.body.appendChild(a); a.click(); a.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        } catch (e) { console.error('downloadCsv:', e); }
    }

    // ====== Clipboard ==========================================================

    async function copyToClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
            showToast('Zkopírováno', text, 'info');
            return true;
        } catch {
            // fallback
            const ta = document.createElement('textarea');
            ta.value = text; document.body.appendChild(ta);
            ta.select(); document.execCommand('copy'); ta.remove();
            showToast('Zkopírováno', text, 'info');
            return true;
        }
    }

    // ====== Loading overlay ====================================================

    const loadingOverlay = {
        show(msg = 'Pracuji…') {
            let o = document.getElementById('admin-overlay');
            if (o) return;
            o = document.createElement('div');
            o.id = 'admin-overlay';
            Object.assign(o.style, {
                position: 'fixed', inset: 0, background: 'rgba(255,255,255,.6)',
                backdropFilter: 'blur(2px)', zIndex: '1050',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontFamily: 'system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif'
            });
            o.innerHTML = `<div style="padding:12px 14px;border:1px solid #e9ecef;background:#fff;border-radius:10px">
        <span class="spinner-border spinner-border-sm me-2"></span>${msg}</div>`;
            document.body.appendChild(o);
        },
        hide() { const o = document.getElementById('admin-overlay'); if (o) o.remove(); }
    };

    // ====== Tooltips / Popovers (pokud je Bootstrap) ==========================

    function initializeTooltips() {
        if (!(window.bootstrap && bootstrap.Tooltip)) return;
        const list = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        [...list].forEach(el => new bootstrap.Tooltip(el));
    }

    function initializePopovers() {
        if (!(window.bootstrap && bootstrap.Popover)) return;
        const list = document.querySelectorAll('[data-bs-toggle="popover"]');
        [...list].forEach(el => new bootstrap.Popover(el));
    }

    // Auto-hide alerts, pokud je přítomný Bootstrap.Alert
    function autoDismissAlerts() {
        const alerts = document.querySelectorAll('.alert[data-auto-dismiss]');
        alerts.forEach(alert => {
            const delay = parseInt(alert.getAttribute('data-auto-dismiss') || '5000', 10);
            setTimeout(() => {
                if (window.bootstrap && bootstrap.Alert) {
                    const bs = bootstrap.Alert.getOrCreateInstance(alert);
                    bs.close();
                } else {
                    alert.remove();
                }
            }, delay);
        });
    }

    // ====== Charts / DataTables (bezpečně) ====================================

    function createChart(canvasId, type, data, options = {}) {
        const ctx = document.getElementById(canvasId);
        if (!ctx || !window.Chart) { console.warn('Chart.js nedostupný'); return null; }
        return new Chart(ctx, { type, data, options: { responsive: true, maintainAspectRatio: false, ...options } });
    }

    function initializeDataTable(tableId, options = {}) {
        const table = document.getElementById(tableId);
        if (!table || !window.$ || !$.fn || !$.fn.DataTable) { console.warn('DataTables nedostupné'); return null; }
        return $(table).DataTable({ responsive: true, ...options });
    }

    // ====== Další utilitky =====================================================

    function previewImage(input, previewId) {
        const preview = document.getElementById(previewId);
        if (!input?.files?.[0] || !preview) return;
        const reader = new FileReader();
        reader.onload = e => { preview.src = e.target.result; preview.style.display = 'block'; };
        reader.readAsDataURL(input.files[0]);
    }

    function validateForm(form) {
        if (!form) return false;
        const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');
        let ok = true;
        inputs.forEach(i => {
            if (!String(i.value || '').trim()) { i.classList.add('is-invalid'); ok = false; }
            else i.classList.remove('is-invalid');
        });
        return ok;
    }

    function toggleSidebar(sidebarId = 'admin-sidebar', mainId = 'admin-main') {
        const s = document.getElementById(sidebarId);
        const m = document.getElementById(mainId);
        if (s) s.classList.toggle('collapsed');
        if (m) m.classList.toggle('sidebar-collapsed');
    }

    function updateUrlParameter(param, value) {
        const url = new URL(window.location.href);
        if (value == null || value === '') url.searchParams.delete(param);
        else url.searchParams.set(param, value);
        window.history.pushState({}, '', url.toString());
    }

    function saveToLocalStorage(key, data) {
        try { localStorage.setItem(key, JSON.stringify(data)); return true; }
        catch (e) { console.error('saveToLocalStorage:', e); return false; }
    }

    function loadFromLocalStorage(key) {
        try { const d = localStorage.getItem(key); return d ? JSON.parse(d) : null; }
        catch (e) { console.error('loadFromLocalStorage:', e); return null; }
    }

    // ====== DOM ready ==========================================================

    document.addEventListener('DOMContentLoaded', () => {
        initializeTooltips();
        initializePopovers();
        autoDismissAlerts();
        const toggleBtn = document.querySelector('[data-toggle-sidebar]');
        if (toggleBtn) toggleBtn.addEventListener('click', () => toggleSidebar());
    });

    // ====== Exporty ============================================================

    const AdminUtils = {
        // UI
        showToast,
        showModal,
        hideModal,
        adminConfirm,

        // UX helpers
        debounce,
        loadingOverlay,

        // IO
        downloadFile,
        downloadCsv,
        copyToClipboard,

        // Data / UI helpers
        validateForm,
        previewImage,
        toggleSidebar,
        updateUrlParameter,
        saveToLocalStorage,
        loadFromLocalStorage,

        // Integrace s knihovnami
        createChart,
        initializeDataTable
    };

    // Jednotný namespace
    window.AdminUtils = Object.assign(window.AdminUtils || {}, AdminUtils);

    // Backward-compat: pokud někdo volá globální funkce
    window.showToast = window.AdminUtils.showToast;
    if (typeof window.showModal !== 'function') window.showModal = window.AdminUtils.showModal;
    if (typeof window.hideModal !== 'function') window.hideModal = window.AdminUtils.hideModal;
    window.adminConfirm = window.AdminUtils.adminConfirm;

})();
