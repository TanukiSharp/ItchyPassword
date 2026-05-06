
// ItchyPassword Passkey Service
// Strict hardware-bound PRF implementation.

window.ItchyPassword = window.ItchyPassword || {};
window.ItchyPassword.Passkey = {
    _assertionTimeoutMs: 65000,
    
    // Constant salt for deriving the PRF key via WebAuthn
    _prfSaltString: 'ItchyPassword-PRF-v1',

    _getPrfSalt: async function() {
        const enc = new TextEncoder();
        return new Uint8Array(await window.crypto.subtle.digest('SHA-256', enc.encode(this._prfSaltString)));
    },

    _toUint8Array: function (value, name) {
        if (value === null || value === undefined) {
            throw new Error('Expected a byte buffer for ' + name + ', got ' + value + '.');
        }
        if (value instanceof Uint8Array) {
            return value.slice();
        }
        if (value instanceof ArrayBuffer) {
            return new Uint8Array(value.slice(0));
        }
        if (Array.isArray(value)) {
            return new Uint8Array(value);
        }
        if (typeof value === 'string') {
            const binary = atob(value);
            const out = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                out[i] = binary.charCodeAt(i);
            }
            return out;
        }
        throw new Error('Unsupported buffer type for ' + name + ': ' + (typeof value) + '.');
    },

    isSupported: async function () {
        try {
            if (typeof window.PublicKeyCredential === 'undefined') {
                return false;
            }

            if (typeof PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable !== 'function') {
                return false;
            }

            const hasPlatformAuthenticator = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
            return hasPlatformAuthenticator === true;
        } catch (e) {
            return false;
        }
    },

    enrollAndWrap: async function (userId, userName, masterKey) {
        const challenge = window.crypto.getRandomValues(new Uint8Array(32));
        const userIdBytes = this._toUint8Array(userId, 'userId');
        const masterKeyBytes = this._toUint8Array(masterKey, 'masterKey');
        const prfSalt = await this._getPrfSalt();

        const publicKeyOptions = {
            challenge: challenge,
            rp: {
                name: 'ItchyPassword',
                id: window.location.hostname
            },
            user: {
                id: userIdBytes,
                name: userName,
                displayName: userName
            },
            pubKeyCredParams: [
                { type: 'public-key', alg: -7 },
                { type: 'public-key', alg: -257 }
            ],
            authenticatorSelection: {
                authenticatorAttachment: 'platform',
                residentKey: 'required',
                requireResidentKey: true,
                userVerification: 'required'
            },
            timeout: 60000,
            attestation: 'none',
            extensions: {
                prf: {
                    eval: {
                        first: prfSalt
                    }
                }
            }
        };

        const credential = await navigator.credentials.create({ publicKey: publicKeyOptions });
        if (!credential) {
            throw new Error('Passkey creation returned no credential.');
        }
        
        const prfResults = credential.getClientExtensionResults().prf;
        if (!prfResults || !prfResults.enabled) {
            throw new Error("NotAllowedError: Authenticator does not support the PRF extension required for secure encryption.");
        }

        const credentialIdBytes = new Uint8Array(credential.rawId);
        
        // At enrollment, PRF extension returns 'enabled: true' but NOT the evaluated key.
        // We must immediately perform an assertion to get the derived key.
        const derivedKeyResult = await this._getAssertionPrf(credentialIdBytes, prfSalt);
        
        const wrapKeyBytes = derivedKeyResult.prfKey;

        try {
            const encryptedMaster = await window.ItchyPassword.Crypto.encryptV3(masterKeyBytes, wrapKeyBytes);
            return {
                credentialId: credentialIdBytes,
                wrappedMasterKey: encryptedMaster
            };
        } finally {
            wrapKeyBytes.fill(0);
        }
    },

    _getAssertionPrf: async function(credentialIdBytes, prfSalt) {
        const publicKeyOptions = {
            challenge: window.crypto.getRandomValues(new Uint8Array(32)),
            rpId: window.location.hostname,
            userVerification: 'required',
            timeout: this._assertionTimeoutMs,
            allowCredentials: [{
                type: 'public-key',
                id: credentialIdBytes
            }],
            extensions: {
                prf: {
                    eval: {
                        first: prfSalt
                    }
                }
            }
        };

        let timerId = null;
        let assertion;
        try {
            assertion = await Promise.race([
                navigator.credentials.get({ publicKey: publicKeyOptions }),
                new Promise((_, reject) => {
                    timerId = setTimeout(() => {
                        reject(new Error('Passkey assertion timed out.'));
                    }, this._assertionTimeoutMs);
                })
            ]);
        } finally {
            if (timerId !== null) {
                clearTimeout(timerId);
            }
        }

        if (!assertion) {
            throw new Error('Passkey assertion returned nothing.');
        }

        const extResults = assertion.getClientExtensionResults();
        if (!extResults.prf || !extResults.prf.results || !extResults.prf.results.first) {
            throw new Error("NotAllowedError: The authenticator did not return the PRF derived key. It may be unsupported on this platform.");
        }

        return {
            assertion: assertion,
            prfKey: new Uint8Array(extResults.prf.results.first)
        };
    },

    unlockAndUnwrap: async function (credentialId, wrappedMasterKey) {
        const credIdBytes = this._toUint8Array(credentialId, 'credentialId');
        const wrappedBytes = this._toUint8Array(wrappedMasterKey, 'wrappedMasterKey');
        const prfSalt = await this._getPrfSalt();

        const derivedKeyResult = await this._getAssertionPrf(credIdBytes, prfSalt);
        const wrapKeyBytes = derivedKeyResult.prfKey;

        try {
            return await window.ItchyPassword.Crypto.decryptV3(wrappedBytes, wrapKeyBytes);
        } catch (e) {
            throw new Error('Failed to decrypt Master Key with passkey: ' + e.message);
        } finally {
            wrapKeyBytes.fill(0);
        }
    }
};
