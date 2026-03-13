// Theme management — persists preference in localStorage and applies via data-theme attribute.
window.themeInterop = {
    /** Applies the saved theme on page load. Called early to prevent flash. */
    init: function () {
        const saved = localStorage.getItem("theme");
        if (saved === "light" || saved === "dark") {
            document.documentElement.setAttribute("data-theme", saved);
        } else {
            document.documentElement.removeAttribute("data-theme");
        }
    },

    /** Returns the current preference: "light", "dark", or "system". */
    get: function () {
        return localStorage.getItem("theme") || "system";
    },

    /** Sets the theme preference. Pass "light", "dark", or "system". */
    set: function (value) {
        if (value === "light" || value === "dark") {
            localStorage.setItem("theme", value);
            document.documentElement.setAttribute("data-theme", value);
        } else {
            localStorage.removeItem("theme");
            document.documentElement.removeAttribute("data-theme");
        }
    }
};

// Apply immediately to prevent flash of wrong theme.
window.themeInterop.init();
