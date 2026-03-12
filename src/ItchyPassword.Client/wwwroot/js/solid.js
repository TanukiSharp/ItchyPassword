/**
 * JS interop for the SOLID vault connector.
 *
 * Responsibilities:
 *   1. OAuth callback relay — intercepts the /solid-oauth-callback page and posts
 *      the code/state to the opener via BroadcastChannel.
 *   2. Popup lifecycle — opens the SOLID sign-in popup and resolves the result.
 *   3. DPoP proof generation — creates ES256 DPoP proof JWTs using SubtleCrypto.
 *      The private key is non-extractable and never leaves the browser's crypto layer.
 *
 * All OIDC discovery, PKCE computation, URL building, token exchange, and storage
 * are handled entirely in C#.
 */

// --- OAuth callback relay ---
// When the SPA loads at /solid-oauth-callback, relay code/state to the opener
// via BroadcastChannel and close the window BEFORE Blazor loads.
(function () {
    if (window.location.pathname !== '/solid-oauth-callback') {
        return;
    }

    var params = new URLSearchParams(window.location.search);
    var channel = new BroadcastChannel('solid-oauth-callback');

    channel.postMessage({
        code: params.get('code'),
        state: params.get('state'),
        error: params.get('error'),
    });

    channel.close();
    document.title = 'Done';
    window.close();

    // Prevent Blazor from loading if window.close() was blocked.
    window.stop();
})();

window.solidInterop = {

    // -------------------------------------------------------------------------
    // DPoP state
    // -------------------------------------------------------------------------

    /** @type {CryptoKeyPair|null} Ephemeral ES256 key pair for this session. */
    _dpopKeyPair: null,

    /** @type {Object|null} Exported public key as JWK (used to embed in proof headers). */
    _dpopPublicJwk: null,

    // -------------------------------------------------------------------------
    // Popup state
    // -------------------------------------------------------------------------

    /** @type {Window|null} Reference to the active sign-in popup. */
    _popup: null,

    /** @type {Promise<string|null>|null} Pending sign-in result promise. */
    _signInPromise: null,

    // -------------------------------------------------------------------------
    // DPoP helpers
    // -------------------------------------------------------------------------

    /**
     * Generates the ES256 DPoP key pair if not already done.
     * The private key is non-extractable by design.
     * @returns {Promise<void>}
     */
    async _ensureDpopKey() {
        if (this._dpopKeyPair) {
            return;
        }

        this._dpopKeyPair = await crypto.subtle.generateKey(
            { name: 'ECDSA', namedCurve: 'P-256' },
            false, // non-extractable private key
            ['sign']
        );

        this._dpopPublicJwk = await crypto.subtle.exportKey('jwk', this._dpopKeyPair.publicKey);
    },

    /**
     * Returns the base64url-encoded SHA-256 thumbprint of the DPoP public key (RFC 7638).
     * Used as the dpop_jkt parameter in authorization requests.
     * @returns {Promise<string>}
     */
    async getDpopKeyThumbprint() {
        await this._ensureDpopKey();

        // Keys must be in lexicographic order per RFC 7638 §3.
        const canonicalJwk = JSON.stringify({
            crv: this._dpopPublicJwk.crv,
            kty: this._dpopPublicJwk.kty,
            x: this._dpopPublicJwk.x,
            y: this._dpopPublicJwk.y,
        });

        const hashBytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(canonicalJwk));
        return this._base64url(new Uint8Array(hashBytes));
    },

    /**
     * Builds a DPoP proof JWT for the given HTTP request (RFC 9449).
     * @param {string} method - HTTP method in any case (will be uppercased).
     * @param {string} url - HTTP target URI without query string or fragment.
     * @param {string|null} accessToken - When set, the ath claim (SHA-256 of AT) is included.
     * @param {string|null} nonce - Server-supplied DPoP nonce to include in the payload.
     * @returns {Promise<string>} Compact-serialized JWT.
     */
    async buildDpopProof(method, url, accessToken, nonce) {
        await this._ensureDpopKey();

        const header = {
            typ: 'dpop+jwt',
            alg: 'ES256',
            jwk: {
                kty: this._dpopPublicJwk.kty,
                crv: this._dpopPublicJwk.crv,
                x: this._dpopPublicJwk.x,
                y: this._dpopPublicJwk.y,
            },
        };

        const payload = {
            jti: crypto.randomUUID(),
            htm: method.toUpperCase(),
            htu: url,
            iat: Math.floor(Date.now() / 1000),
        };

        if (nonce) {
            payload['nonce'] = nonce;
        }

        if (accessToken) {
            // ath = base64url(SHA-256(ASCII(access_token))) per RFC 9449 §4.2.
            const atBytes = new TextEncoder().encode(accessToken);
            const hashBytes = await crypto.subtle.digest('SHA-256', atBytes);
            payload['ath'] = this._base64url(new Uint8Array(hashBytes));
        }

        const headerB64 = this._base64urlStr(JSON.stringify(header));
        const payloadB64 = this._base64urlStr(JSON.stringify(payload));
        const signingInput = `${headerB64}.${payloadB64}`;

        const sigBytes = await crypto.subtle.sign(
            { name: 'ECDSA', hash: 'SHA-256' },
            this._dpopKeyPair.privateKey,
            new TextEncoder().encode(signingInput)
        );

        return `${signingInput}.${this._base64url(new Uint8Array(sigBytes))}`;
    },

    /**
     * Base64url-encodes a Uint8Array (no padding, URL-safe characters).
     * @param {Uint8Array} bytes
     * @returns {string}
     */
    _base64url(bytes) {
        let bin = '';
        for (const b of bytes) {
            bin += String.fromCharCode(b);
        }
        return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
    },

    /**
     * UTF-8 encodes a string and base64url-encodes the result.
     * @param {string} str
     * @returns {string}
     */
    _base64urlStr(str) {
        return this._base64url(new TextEncoder().encode(str));
    },

    // -------------------------------------------------------------------------
    // Popup management
    // -------------------------------------------------------------------------

    /**
     * Opens a popup for SOLID OIDC sign-in and listens for the callback
     * result via BroadcastChannel.
     * MUST be called synchronously from a user-gesture context so the popup
     * is not blocked by the browser.
     * @param {string} url - The fully constructed authorization URL.
     */
    openPopup(url) {
        this._popup = window.open(url, 'solid-auth', 'width=500,height=600');

        if (!this._popup) {
            this._signInPromise = Promise.resolve(null);
            return;
        }

        this._signInPromise = new Promise((resolve) => {
            const channel = new BroadcastChannel('solid-oauth-callback');

            // Resolve null if the user closes the popup without completing sign-in.
            const timeoutId = setTimeout(() => {
                channel.close();
                resolve(null);
            }, 5 * 60 * 1000);

            channel.onmessage = (event) => {
                clearTimeout(timeoutId);
                channel.close();

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
     * Awaits the result of the previously opened sign-in popup.
     * @returns {Promise<string|null>} JSON string with { code, state }, or null on failure/cancellation.
     */
    async awaitResult() {
        if (!this._signInPromise) {
            return null;
        }

        const result = await this._signInPromise;
        this._signInPromise = null;
        return result;
    },
};
