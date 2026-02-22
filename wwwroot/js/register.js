/**
 * REGISTER.JS - Trang đăng ký MTU Social
 */
(function () {
    'use strict';

    // Floating particles
    const container = document.getElementById('particles');
    if (container) {
        for (let i = 0; i < 18; i++) {
            const p = document.createElement('div');
            p.className = 'particle';
            p.style.cssText = [
                'left:' + (Math.random() * 100) + '%;',
                'bottom: -10px;',
                'width:' + (Math.random() * 5 + 3) + 'px;',
                'height:' + (Math.random() * 5 + 3) + 'px;',
                'animation-duration:' + (Math.random() * 8 + 6) + 's;',
                'animation-delay:' + (Math.random() * 6) + 's;',
                'opacity:' + (Math.random() * 0.4 + 0.1) + ';',
            ].join('');
            container.appendChild(p);
        }
    }

    // Real-time email validation
    const emailInput = document.getElementById('Email');
    const MTU_REGEX = /^[a-zA-Z0-9._%+\-]+@mtu\.edu\.vn$/;

    if (emailInput) {
        emailInput.addEventListener('input', function () {
            const val = emailInput.value.trim();
            const valid = MTU_REGEX.test(val);

            emailInput.classList.remove('input-valid', 'input-error');

            if (val.length === 0) return;

            if (valid) {
                emailInput.classList.add('input-valid');
            } else {
                emailInput.classList.add('input-error');
            }
        });
    }

    // Loading state + client validation on submit
    const form = document.getElementById('regForm');
    const submitBtn = document.getElementById('submitBtn');

    if (form && submitBtn && emailInput) {
        form.addEventListener('submit', function (e) {
            const val = emailInput.value.trim();
            const valid = MTU_REGEX.test(val);

            if (!valid) {
                e.preventDefault();
                emailInput.classList.add('input-error');
                emailInput.focus();
                return;
            }

            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i><span>Đang gửi...</span>';
        });
    }

})();
