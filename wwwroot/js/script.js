(function () {
    function getThemeToggleIcon() {
        return document.querySelector('.toggle-light-mode i');
    }

    function applyTheme(theme) {
        const nextTheme = theme === 'light' ? 'light' : 'dark';
        const body = document.body;
        const themeIcon = getThemeToggleIcon();

        body.classList.remove('light', 'dark');
        body.classList.add(nextTheme);

        if (!themeIcon) {
            return;
        }

        themeIcon.classList.remove('fa-sun', 'fa-moon');
        themeIcon.classList.add(nextTheme === 'light' ? 'fa-moon' : 'fa-sun');
    }

    function toggleTheme() {
        const nextTheme = document.body.classList.contains('light') ? 'dark' : 'light';
        applyTheme(nextTheme);
        localStorage.setItem('theme', nextTheme);
    }

    function setSidePanelState(isOpen) {
        const sidePanel = document.getElementById('sidePanel');
        const menuIcon = document.querySelector('.menu-toggle i');
        const containersToShift = [
            document.querySelector('.container'),
            document.querySelector('.shop-container'),
            document.querySelector('.product-page-container'),
            document.querySelector('.review-page-content')
        ];

        if (sidePanel) {
            sidePanel.classList.toggle('open', isOpen);
        }

        if (menuIcon) {
            menuIcon.classList.toggle('fa-bars', !isOpen);
            menuIcon.classList.toggle('fa-times', isOpen);
        }

        containersToShift.forEach(container => {
            if (container) {
                container.classList.toggle('shifted', isOpen);
            }
        });
    }

    function autoResizeTextareas() {
        document.querySelectorAll('textarea').forEach(textarea => {
            textarea.style.height = 'auto';
            textarea.style.height = textarea.scrollHeight + 'px';
        });
    }

    function updateCartBadge(count) {
        const badge = document.querySelector('.cart-badge');
        if (!badge) {
            return;
        }

        if (typeof count === 'number') {
            badge.textContent = String(count);
            return;
        }

        const currentValue = parseInt(badge.textContent || '0', 10);
        badge.textContent = Number.isNaN(currentValue) ? '0' : String(currentValue);
    }

    window.toggleSidePanel = function toggleSidePanel() {
        const sidePanel = document.getElementById('sidePanel');
        const isOpen = !(sidePanel && sidePanel.classList.contains('open'));
        setSidePanelState(isOpen);
    };

    window.updateCartBadge = updateCartBadge;

    document.addEventListener('DOMContentLoaded', () => {
        applyTheme(localStorage.getItem('theme') || 'dark');
        setSidePanelState(false);
        updateCartBadge();
        autoResizeTextareas();

        const themeToggle = document.querySelector('.toggle-light-mode');
        if (themeToggle) {
            themeToggle.addEventListener('click', toggleTheme);
        }

        document.querySelectorAll('textarea').forEach(textarea => {
            textarea.addEventListener('input', autoResizeTextareas);
        });

        document.addEventListener('keydown', event => {
            if (event.key !== 'Escape') {
                return;
            }

            const imagePopup = document.getElementById('image-popup');
            if (imagePopup && typeof window.closePopup === 'function') {
                window.closePopup();
            }

            const loginModal = document.getElementById('login-modal');
            const registerModal = document.getElementById('register-modal');
            const searchModal = document.getElementById('search-modal');

            if (loginModal && typeof window.closeLogin === 'function') {
                window.closeLogin();
            }

            if (registerModal && typeof window.closeRegister === 'function') {
                window.closeRegister();
            }

            if (searchModal && typeof window.closeSearchPopup === 'function') {
                window.closeSearchPopup();
            }
        });
    });

    window.addEventListener('resize', autoResizeTextareas);
})();
