(function() {
    'use strict';

    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {});
        }
    }

    window.ContactsManager = {
        init: init
    };

    init();

})();
