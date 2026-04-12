// DOM interop helpers — sets element styles via CSSOM (CSP-compliant, unlike inline style attributes).
window.domInterop = {
    setStyle: (el, prop, value) => {
        if (el && el.style) {
            el.style.setProperty(prop, value);
        }
    },
    setWidthAnimated: (el, widthPercent, skipTransition) => {
        if (!el || !el.style) {
            return;
        }
        if (skipTransition) {
            el.style.setProperty('transition', 'none');
            el.style.setProperty('width', widthPercent + '%');
            void el.offsetHeight;
            el.style.removeProperty('transition');
        } else {
            el.style.setProperty('width', widthPercent + '%');
        }
    },
    focusOnBlur: (source, target) => {
        if (source && target) {
            source.addEventListener('blur', () => target.focus());
        }
    }
};
