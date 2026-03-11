// Solid Auth Popup Interceptor
(function () {
    const href = window.location.href;
    // Check if we are running in the popup and returning from the IDP
    if (href.includes('state=') && (href.includes('code=') || href.includes('error='))) {
        const channel = new BroadcastChannel('solid-auth-channel');
        channel.postMessage({ type: href.includes('error=') ? 'solid-auth-error' : 'solid-auth-success', url: href });

        // Halt the rest of the Blazor app from loading inside the popup
        if (window.stop) window.stop();
        document.title = "Authentication Callback";
        document.documentElement.innerHTML = '<div style="margin:50px;text-align:center;font-family:sans-serif;"><p>Completing Solid authentication...</p><p>This window should close automatically.</p></div>';

        setTimeout(() => { window.close(); }, 100);
    }
})();

// We dynamically inject the script tag to avoid a build step and keep Blazor lean.
let solidClientAuthnBrowser = null;

async function loadLibrary() {
    if (!solidClientAuthnBrowser) {
        if (window.solidClientAuthentication) {
            solidClientAuthnBrowser = window.solidClientAuthentication;
            return solidClientAuthnBrowser;
        }

        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = 'https://unpkg.com/@inrupt/solid-client-authn-browser@2.2.0/dist/solid-client-authn.bundle.js';
            script.onload = () => {
                solidClientAuthnBrowser = window.solidClientAuthentication;
                resolve(solidClientAuthnBrowser);
            };
            script.onerror = () => {
                reject(new Error("Failed to load solid-client-authn-browser bundle"));
            };
            document.head.appendChild(script);
        });
    }
    return solidClientAuthnBrowser;
}

window.solidVault = {
    login: async function (podUrl) {
        try {
            const inrupt = await loadLibrary();

            // Check if already logged in (session restore)
            const session = inrupt.getDefaultSession();

            // Handle incoming redirect if it happened previously
            await session.handleIncomingRedirect({ restorePreviousSession: true });

            if (session.info.isLoggedIn) {
                return true;
            }

            // We use a Promise to wait for the popup to complete the flow.
            return new Promise((resolve) => {
                const channel = new BroadcastChannel('solid-auth-channel');
                let popupWindow = null;

                channel.onmessage = async (event) => {
                    const message = event.data;
                    if (message.type === 'solid-auth-success') {
                        channel.close();
                        if (popupWindow) popupWindow.close();

                        try {
                            await session.handleIncomingRedirect(message.url);
                            resolve(session.info.isLoggedIn);
                        } catch (error) {
                            console.error('Failed to handle incoming redirect from popup:', error);
                            resolve(false);
                        }
                    } else if (message.type === 'solid-auth-error') {
                        channel.close();
                        if (popupWindow) popupWindow.close();
                        resolve(false);
                    }
                };

                // The library attempts to redirect. We intercept the URL by overriding window.location.href.
                // However, `inrupt.login` internally sets window.location.href.
                // We intercept the URL natively by opening a popup.
                // Redirect back to the main app URL, our interceptor script in index.html will catch it and close the popup.
                const redirectUrl = window.location.origin + '/';

                // Login redirects the page. But we want a popup!
                // To achieve this with @inrupt/solid-client-authn-browser, we pass handleRedirect:
                inrupt.login({
                    oidcIssuer: podUrl,
                    redirectUrl: redirectUrl,
                    clientName: "ItchyPassword",
                    handleRedirect: (url) => {
                        // Instead of full page redirect, open it in a popup
                        popupWindow = window.open(url, 'solid-auth-popup', 'width=600,height=800');

                        // Check if the user closed the popup prematurely
                        const checkClosed = setInterval(() => {
                            if (popupWindow && popupWindow.closed) {
                                clearInterval(checkClosed);
                                channel.close();
                                resolve(false);
                            }
                        }, 500);
                    }
                }).catch(err => {
                    console.error("Solid login error:", err);
                    resolve(false);
                });
            });
        } catch (error) {
            console.error('Solid login error:', error);
            return false;
        }
    },

    loadVault: async function (fileUrl) {
        const inrupt = await loadLibrary();
        const session = inrupt.getDefaultSession();

        if (!session.info.isLoggedIn) {
            throw new Error('Not logged in to Solid.');
        }

        const response = await session.fetch(fileUrl, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (response.status === 404) {
            return ''; // File not found, allow creating a new one
        }

        if (!response.ok) {
            throw new Error(`Failed to load vault: ${response.status} ${response.statusText}`);
        }

        return await response.text();
    },

    saveVault: async function (fileUrl, content) {
        const inrupt = await loadLibrary();
        const session = inrupt.getDefaultSession();

        if (!session.info.isLoggedIn) {
            throw new Error('Not logged in to Solid.');
        }

        const response = await session.fetch(fileUrl, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'If-None-Match': '*' // Creates if not exists
            },
            body: content
        });

        // Depending on the server, we might need to use path/patch or a simple PUT.
        // PUT replaces the entire resource.

        if (!response.ok) {
            throw new Error(`Failed to save vault: ${response.status} ${response.statusText}`);
        }
    },

    logout: async function () {
        try {
            const inrupt = await loadLibrary();
            const session = inrupt.getDefaultSession();
            if (session.info.isLoggedIn) {
                await session.logout();
            }
        } catch (error) {
            console.error('Solid logout error:', error);
        }
    }
};
