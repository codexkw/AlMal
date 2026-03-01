// ============================================================
// Al-Mal Channel - Site JavaScript
// ============================================================

// ----------------------------------------------------------
// Dark / Light Mode Toggle
// ----------------------------------------------------------

/**
 * Initialize the theme from localStorage or default to 'dark'.
 * Called immediately on page load (before Alpine).
 */
(function initTheme() {
    const saved = localStorage.getItem('theme');
    const theme = saved || 'dark';
    document.documentElement.setAttribute('data-theme', theme);
})();

/**
 * Alpine.js component for theme toggle button.
 * Usage: x-data="themeToggle()"
 */
function themeToggle() {
    return {
        isDark: document.documentElement.getAttribute('data-theme') === 'dark',

        toggle() {
            // Add transitioning class for smooth CSS transitions
            document.body.classList.add('theme-transitioning');

            this.isDark = !this.isDark;
            const newTheme = this.isDark ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);

            // Remove transitioning class after animation completes
            setTimeout(() => {
                document.body.classList.remove('theme-transitioning');
            }, 350);
        }
    };
}

// ----------------------------------------------------------
// HTMX Configuration
// ----------------------------------------------------------

document.addEventListener('DOMContentLoaded', function () {
    // HTMX: Add anti-forgery token to all requests
    document.body.addEventListener('htmx:configRequest', function (event) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            event.detail.headers['RequestVerificationToken'] = token.value;
        }
    });

    // HTMX: Show loading skeleton during requests
    document.body.addEventListener('htmx:beforeRequest', function (event) {
        const target = event.detail.target;
        if (target && target.dataset.loadingSkeleton) {
            target.classList.add('htmx-loading');
        }
    });

    document.body.addEventListener('htmx:afterRequest', function (event) {
        const target = event.detail.target;
        if (target) {
            target.classList.remove('htmx-loading');
        }
    });
});

// ----------------------------------------------------------
// Utility: Format KWD Currency
// ----------------------------------------------------------

/**
 * Format a number as Kuwaiti Dinar (KWD) with 3 decimal places.
 * @param {number} value - The numeric value.
 * @returns {string} Formatted string, e.g. "0.345 د.ك"
 */
function formatKWD(value) {
    if (value == null || isNaN(value)) return '---';
    return value.toFixed(3) + ' د.ك';
}

/**
 * Format a percentage value.
 * @param {number} value - The percentage value.
 * @returns {string} Formatted string, e.g. "+2.45%" or "-1.20%"
 */
function formatPercent(value) {
    if (value == null || isNaN(value)) return '---';
    const sign = value >= 0 ? '+' : '';
    return sign + value.toFixed(2) + '%';
}

/**
 * Get CSS class based on a numeric value (positive/negative).
 * @param {number} value - The value to evaluate.
 * @returns {string} CSS class name.
 */
function priceColorClass(value) {
    if (value > 0) return 'text-positive';
    if (value < 0) return 'text-negative';
    return '';
}
