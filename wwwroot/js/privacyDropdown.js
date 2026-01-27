(function() {
    'use strict';

    const privacyBtn = document.getElementById('privacy-btn');
    const privacyMenu = document.getElementById('privacy-menu');
    const privacyInput = document.getElementById('post-privacy-input');
    const privacyIcon = document.getElementById('privacy-icon');
    const privacyText = document.getElementById('privacy-text');

    if (!privacyBtn || !privacyMenu || !privacyInput) return;

    const privacyOptions = {
        'public': {
            icon: 'fa-globe',
            text: 'Công khai'
        },
        'friends': {
            icon: 'fa-user-group',
            text: 'Bạn bè'
        },
        'private': {
            icon: 'fa-lock',
            text: 'Riêng tư'
        }
    };

    privacyBtn.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        privacyMenu.classList.toggle('show');
    });

    document.addEventListener('click', function(e) {
        if (!e.target.closest('.privacy-dropdown')) {
            privacyMenu.classList.remove('show');
        }
    });

    const options = privacyMenu.querySelectorAll('.privacy-option');
    options.forEach(function(option) {
        option.addEventListener('click', function() {
            const value = this.getAttribute('data-value');
            
            options.forEach(opt => opt.classList.remove('active'));
            this.classList.add('active');
            
            privacyInput.value = value;
            
            const config = privacyOptions[value];
            if (config) {
                privacyIcon.className = 'feed-btn-icon fa-solid ' + config.icon;
                privacyText.textContent = config.text;
            }
            
            privacyMenu.classList.remove('show');
        });
    });

    const defaultOption = privacyMenu.querySelector('[data-value="public"]');
    if (defaultOption) {
        defaultOption.classList.add('active');
    }
})();
