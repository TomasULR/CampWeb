// Admin Panel JavaScript Functions
// This file contains utility functions for the admin panel

// Toast notification system
window.showToast = function(title, message, type) {
    // Enhanced toast implementation
    const toastContainer = document.getElementById('toast-container') || createToastContainer();

    const toast = document.createElement('div');
    const toastId = 'toast-' + Date.now();
    toast.id = toastId;
    toast.className = `alert alert-${getBootstrapClass(type)} alert-dismissible fade show toast-item`;
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-icon me-3">
                <i class="fas ${getToastIcon(type)}"></i>
            </div>
            <div class="flex-grow-1">
                <div class="toast-title fw-bold">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <button type="button" class="btn-close" onclick="removeToast('${toastId}')"></button>
        </div>
    `;

    toastContainer.appendChild(toast);

    // Auto remove after 5 seconds
    setTimeout(() => {
        removeToast(toastId);
    }, 5000);

    // Animate in
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);
};
function getToastTypeClass(type) {
    switch (type) {
        case 'success': return 'text-success';
        case 'error': return 'text-danger';
        case 'warning': return 'text-warning';
        default: return 'text-info';
    }
}

function getToastIcon(type) {
    switch (type) {
        case 'success': return 'fas fa-check-circle text-success';
        case 'error': return 'fas fa-exclamation-circle text-danger';
        case 'warning': return 'fas fa-exclamation-triangle text-warning';
        default: return 'fas fa-info-circle text-info';
    }
}

// Modal utilities
function showModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
    }
}

function hideModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        const bsModal = bootstrap.Modal.getInstance(modal);
        if (bsModal) {
            bsModal.hide();
        }
    }
}

// File download utilities
function downloadFile(url, filename) {
    const link = document.createElement('a');
    link.href = url;
    link.download = filename || '';
    link.target = '_blank';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function downloadCsv(filename, csvContent) {
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    if (link.download !== undefined) {
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
}

// Sidebar toggle functionality
function toggleSidebar() {
    const sidebar = document.getElementById('admin-sidebar');
    const mainContent = document.getElementById('admin-main');
    
    if (sidebar && mainContent) {
        sidebar.classList.toggle('collapsed');
        mainContent.classList.toggle('sidebar-collapsed');
    }
}

// Auto-hide alerts after delay
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert[data-auto-dismiss]');
    alerts.forEach(alert => {
        const delay = parseInt(alert.getAttribute('data-auto-dismiss')) || 5000;
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, delay);
    });
});

// Form validation utilities
function validateForm(formElement) {
    if (!formElement) return false;
    
    const inputs = formElement.querySelectorAll('input[required], select[required], textarea[required]');
    let isValid = true;
    
    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.classList.add('is-invalid');
            isValid = false;
        } else {
            input.classList.remove('is-invalid');
        }
    });
    
    return isValid;
}

// Initialize tooltips
function initializeTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
}

// Initialize popovers
function initializePopovers() {
    const popoverTriggerList = document.querySelectorAll('[data-bs-toggle="popover"]');
    const popoverList = [...popoverTriggerList].map(popoverTriggerEl => new bootstrap.Popover(popoverTriggerEl));
}

// Auto-initialize components when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeTooltips();
    initializePopovers();
    
    // Set up sidebar toggle button
    const toggleButton = document.querySelector('[data-toggle-sidebar]');
    if (toggleButton) {
        toggleButton.addEventListener('click', toggleSidebar);
    }
});

// Chart utilities (if using Chart.js)
function createChart(canvasId, type, data, options = {}) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;
    
    return new Chart(ctx, {
        type: type,
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            ...options
        }
    });
}

// DataTable utilities (if using DataTables)
function initializeDataTable(tableId, options = {}) {
    const table = document.getElementById(tableId);
    if (!table) return null;
    
    return $(table).DataTable({
        responsive: true,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/cs.json'
        },
        ...options
    });
}

// Image preview functionality
function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    if (!input.files || !input.files[0] || !preview) return;
    
    const reader = new FileReader();
    reader.onload = function(e) {
        preview.src = e.target.result;
        preview.style.display = 'block';
    };
    reader.readAsDataURL(input.files[0]);
}

// Loading state utilities
function showLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Načítání...</span>
                </div>
                <p class="mt-2">Načítání...</p>
            </div>
        `;
    }
}

function hideLoading(elementId, content = '') {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = content;
    }
}

// Confirmation dialogs
function confirmAction(message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

// URL utilities
function updateUrlParameter(param, value) {
    const url = new URL(window.location);
    if (value) {
        url.searchParams.set(param, value);
    } else {
        url.searchParams.delete(param);
    }
    window.history.pushState({}, '', url);
}

// Local storage utilities
function saveToLocalStorage(key, data) {
    try {
        localStorage.setItem(key, JSON.stringify(data));
        return true;
    } catch (e) {
        console.error('Error saving to localStorage:', e);
        return false;
    }
}

function loadFromLocalStorage(key) {
    try {
        const data = localStorage.getItem(key);
        return data ? JSON.parse(data) : null;
    } catch (e) {
        console.error('Error loading from localStorage:', e);
        return null;
    }
}

// Export functions for use with Blazor
window.AdminUtils = {
    showToast,
    showModal,
    hideModal,
    downloadFile,
    downloadCsv,
    toggleSidebar,
    validateForm,
    previewImage,
    showLoading,
    hideLoading,
    confirmAction,
    updateUrlParameter,
    saveToLocalStorage,
    loadFromLocalStorage
};