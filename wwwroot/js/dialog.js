/**
 * MTUDialog - Custom Dialog & Toast System
 * Thay thế confirm() và alert() mặc định của trình duyệt
 * MTU Social Network - Lo-Fi Style
 */
(function (global) {
    'use strict';

    // ==========================================
    // TOAST SYSTEM
    // ==========================================

    let toastStack = null;

    function getToastStack() {
        if (!toastStack) {
            toastStack = document.createElement('div');
            toastStack.className = 'mtu-toast-stack';
            document.body.appendChild(toastStack);
        }
        return toastStack;
    }

    const TOAST_ICONS = {
        success: '<i class="fa-solid fa-circle-check"></i>',
        error: '<i class="fa-solid fa-circle-xmark"></i>',
        warning: '<i class="fa-solid fa-triangle-exclamation"></i>',
        info: '<i class="fa-solid fa-circle-info"></i>',
    };

    /**
     * Hiển thị toast notification
     * @param {string} message - Nội dung thông báo
     * @param {'success'|'error'|'warning'|'info'} type - Loại thông báo
     * @param {number} duration - Thời gian hiển thị (ms), mặc định 3000
     */
    function showToast(message, type = 'info', duration = 3000) {
        const stack = getToastStack();

        const toast = document.createElement('div');
        toast.className = 'mtu-toast ' + type;
        toast.innerHTML =
            '<span class="mtu-toast-icon">' + (TOAST_ICONS[type] || TOAST_ICONS.info) + '</span>' +
            '<div class="mtu-toast-body"><div class="mtu-toast-text">' + escapeHtml(message) + '</div></div>';

        stack.appendChild(toast);

        // Trigger animation
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                toast.classList.add('show');
            });
        });

        // Auto remove
        setTimeout(() => {
            hideToast(toast);
        }, duration);
    }

    function hideToast(toast) {
        toast.classList.add('hide');
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) toast.parentNode.removeChild(toast);
        }, 350);
    }

    // ==========================================
    // DIALOG SYSTEM
    // ==========================================

    const DIALOG_ICONS = {
        warn: '<i class="fa-solid fa-triangle-exclamation"></i>',
        error: '<i class="fa-solid fa-circle-xmark"></i>',
        info: '<i class="fa-solid fa-circle-info"></i>',
        success: '<i class="fa-solid fa-circle-check"></i>',
        delete: '<i class="fa-solid fa-trash"></i>',
    };

    /**
     * Hiển thị hộp thoại xác nhận (thay thế confirm())
     * @param {object} options
     * @param {string} options.title - Tiêu đề
     * @param {string} options.message - Nội dung
     * @param {'warn'|'error'|'info'|'delete'} options.type - Loại icon
     * @param {string} options.confirmText - Text nút xác nhận (mặc định: "Xác nhận")
     * @param {string} options.cancelText - Text nút hủy (mặc định: "Hủy")
     * @param {boolean} options.danger - Nút xác nhận màu đỏ
     * @returns {Promise<boolean>} - true nếu xác nhận, false nếu hủy
     */
    function confirm(options) {
        return new Promise((resolve) => {
            const {
                title = 'Xác nhận',
                message = 'Bạn có chắc chắn không?',
                type = 'warn',
                confirmText = 'Xác nhận',
                cancelText = 'Hủy',
                danger = false,
            } = options;

            const overlay = document.createElement('div');
            overlay.className = 'mtu-dialog-overlay';
            overlay.innerHTML =
                '<div class="mtu-dialog-box" role="dialog" aria-modal="true" aria-labelledby="mtu-dialog-title">' +
                '<div class="mtu-dialog-icon ' + type + '">' + (DIALOG_ICONS[type] || DIALOG_ICONS.warn) + '</div>' +
                '<div class="mtu-dialog-title" id="mtu-dialog-title">' + escapeHtml(title) + '</div>' +
                '<div class="mtu-dialog-message">' + escapeHtml(message) + '</div>' +
                '<div class="mtu-dialog-actions">' +
                '<button class="mtu-dialog-btn mtu-dialog-btn-cancel" id="mtu-dialog-cancel">' + escapeHtml(cancelText) + '</button>' +
                '<button class="mtu-dialog-btn mtu-dialog-btn-confirm ' + (danger ? 'danger' : '') + '" id="mtu-dialog-confirm">' + escapeHtml(confirmText) + '</button>' +
                '</div>' +
                '</div>';

            document.body.appendChild(overlay);

            // Focus trap
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    overlay.classList.add('show');
                    overlay.querySelector('#mtu-dialog-cancel').focus();
                });
            });

            function cleanup(result) {
                overlay.classList.remove('show');
                setTimeout(() => {
                    if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
                }, 250);
                resolve(result);
            }

            overlay.querySelector('#mtu-dialog-confirm').addEventListener('click', () => cleanup(true));
            overlay.querySelector('#mtu-dialog-cancel').addEventListener('click', () => cleanup(false));

            // Close on overlay click
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) cleanup(false);
            });

            // Keyboard: Escape = cancel, Enter = confirm
            overlay.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') cleanup(false);
                if (e.key === 'Enter') cleanup(true);
            });
        });
    }

    /**
     * Hiển thị thông báo đơn giản (thay thế alert())
     * @param {string} message - Nội dung
     * @param {'success'|'error'|'warning'|'info'} type - Loại
     */
    function alert(message, type = 'info') {
        // Dùng toast thay vì modal cho alert()
        const duration = type === 'error' ? 4500 : 3000;
        showToast(message, type, duration);
    }

    // ==========================================
    // HELPERS
    // ==========================================

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ==========================================
    // EXPOSE GLOBAL API
    // ==========================================

    global.MTUDialog = {
        confirm,
        alert,
        toast: showToast,
        success: (msg) => showToast(msg, 'success'),
        error: (msg) => showToast(msg, 'error', 4500),
        warning: (msg) => showToast(msg, 'warning'),
        info: (msg) => showToast(msg, 'info'),
    };

})(window);
