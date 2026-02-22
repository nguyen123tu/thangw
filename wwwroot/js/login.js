/**
 * LOGIN.JS - Trang đăng nhập MTU Social
 */
(function () {
    'use strict';

    // Toggle password visibility
    const pwdInput = document.getElementById('passwordInput');
    const toggleBtn = document.getElementById('toggleBtn');
    const tIcon = document.getElementById('toggleIcon');

    if (toggleBtn && pwdInput) {
        toggleBtn.addEventListener('click', function () {
            const show = pwdInput.type === 'password';
            pwdInput.type = show ? 'text' : 'password';
            tIcon.className = show ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
        });
    }

    // Loading state on submit
    const form = document.getElementById('loginForm');
    const submitBtn = document.getElementById('submitBtn');

    if (form && submitBtn) {
        form.addEventListener('submit', function () {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i><span>Đang đăng nhập...</span>';
        });
    }

    // Custom checkbox visual sync
    const cb = document.getElementById('rememberMe');
    const box = document.getElementById('customBox');

    if (cb && box) {
        cb.addEventListener('change', function () {
            box.textContent = cb.checked ? '✓' : '';
        });
    }

})();
