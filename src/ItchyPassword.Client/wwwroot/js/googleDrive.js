/**
 * Minimal JS interop for the Google Drive vault connector.
 * Only handles browser APIs not accessible from C#: opening a popup window
 * and listening for the OAuth callback result via BroadcastChannel.
 *
 * All cryptographic operations (PKCE), URL construction, state validation,
 * and token exchange are handled entirely in C#.
 */

// --- OAuth callback interception ---
// When the SPA fallback serves index.html for /google-oauth-callback,
// relay the code/state via BroadcastChannel and close the popup
// BEFORE Blazor or any other heavy script loads.
(function () {
    // Use endsWith so the check works both at the root (/google-oauth-callback)
    // and under a subpath (e.g. /itchypassword/google-oauth-callback on GitHub Pages).
    var path = window.location.pathname.replace(/\/$/, '');
    if (!path.endsWith('/google-oauth-callback')) {
        return;
    }

    var params = new URLSearchParams(window.location.search);
    var channel = new BroadcastChannel('google-oauth-callback');

    channel.postMessage({
        code: params.get('code'),
        state: params.get('state'),
        error: params.get('error'),
    });

    channel.close();
    document.title = 'Done';
    window.close();

    // If window.close() was blocked (not opened by script), prevent Blazor from loading.
    window.stop();
})();

window.googleDriveInterop = {
    /** @type {Window|null} Reference to the sign-in popup window. */
    _popup: null,

    /** @type {Promise<string|null>|null} Pending sign-in result. */
    _signInPromise: null,

    /** @type {Function|null} Resolves the pending promise as null when called. */
    _cancelFn: null,

    /**
     * Opens a popup navigating to the given authorization URL and listens
     * for the OAuth callback result via postMessage.
     * MUST be called synchronously from a user gesture context.
     * @param {string} url - The fully constructed Google OAuth authorization URL.
     */
    openPopup(url) {
        this._popup = window.open(url, 'google-auth', 'width=500,height=600');

        if (!this._popup) {
            this._signInPromise = Promise.resolve(null);
            return;
        }

        // Use BroadcastChannel instead of window.opener.postMessage because
        // Google's Cross-Origin-Opener-Policy header severs the opener
        // reference and blocks popup.closed polling.
        this._signInPromise = new Promise((resolve) => {
            const channel = new BroadcastChannel('google-oauth-callback');

            const cleanup = () => {
                clearTimeout(timeoutId);
                channel.close();
                this._cancelFn = null;
            };

            // Timeout: resolve null if the user closes the popup without
            // completing sign-in. Since we cannot poll popup.closed under
            // COOP, we use a generous timeout instead.
            const timeoutId = setTimeout(() => {
                cleanup();
                resolve(null);
            }, 5 * 60 * 1000);

            // Allow programmatic cancellation from C# via cancelSignIn().
            this._cancelFn = () => {
                cleanup();
                resolve(null);

                try {
                    this._popup?.close();
                } catch {
                    // Popup may already be closed.
                }
            };

            channel.onmessage = (event) => {
                cleanup();

                if (event.data.error) {
                    resolve(null);
                    return;
                }

                resolve(JSON.stringify({
                    code: event.data.code,
                    state: event.data.state,
                }));

                try {
                    this._popup?.close();
                } catch {
                    // Popup may already be closed.
                }
            };
        });
    },

    /**
     * Awaits the result of a previously initiated sign-in popup.
     * @returns {Promise<string|null>} JSON string with { code, state }, or null on failure.
     */
    async awaitResult() {
        if (!this._signInPromise) {
            return null;
        }

        const result = await this._signInPromise;
        this._signInPromise = null;
        return result;
    },

    /**
     * Cancels a pending sign-in by resolving the promise as null and closing the popup.
     * Called from C# when the user explicitly cancels the loading flow.
     */
    cancelSignIn() {
        if (this._cancelFn) {
            this._cancelFn();
        }
    },

};
