// ItchyPassword Crypto Service
// Wraps SubtleCrypto for Blazor

window.ItchyPassword = window.ItchyPassword || {};
window.ItchyPassword.Crypto = {
    // Basic Key Derivation (PBKDF2 -> AES-GCM Key Bytes)
    getDerivedBytes: async function (password, salt, iterations) {
        try {
            const baseKey = await window.crypto.subtle.importKey(
                'raw',
                password,
                { name: 'PBKDF2' }, // Must be object for import
                false,
                ['deriveKey']
            );

            const algorithm = {
                name: 'PBKDF2',
                hash: 'SHA-512',
                iterations: iterations,
                salt: salt
            };

            const derivedKeyType = {
                name: 'AES-GCM',
                length: 256
            };

            const derivedKey = await window.crypto.subtle.deriveKey(
                algorithm,
                baseKey,
                derivedKeyType,
                true, // exportable
                ['encrypt', 'decrypt']
            );

            const exportedKey = await window.crypto.subtle.exportKey('raw', derivedKey);
            return new Uint8Array(exportedKey);
        } catch (e) {
            console.error('Crypto Error:', e);
            throw e;
        }
    },

    // Password V1 Generation
    // HKDF(PBKDF2(100k), HMAC-SHA512)
    generatePasswordV1: async function (privatePart, publicPart) {
        try {
            // 1. Get Derived Key (PBKDF2 -> AES-GCM) with 100,000 iterations
            const derivedKey = await this.getDerivedBytes(privatePart, publicPart, 100000);

            // 2. Import as HMAC Key
            const hmacKey = await window.crypto.subtle.importKey(
                'raw',
                derivedKey,
                { name: 'HMAC', hash: { name: 'SHA-512' } },
                false,
                ['sign']
            );

            // 3. Sign 'Password' (hkdfPurpose)
            const hkdfPurpose = 'Password';
            const purposeBytes = new TextEncoder().encode(hkdfPurpose);

            const signature = await window.crypto.subtle.sign(
                'HMAC',
                hmacKey,
                purposeBytes
            );

            return new Uint8Array(signature);

        } catch (e) {
            console.error('Crypto V1 Error:', e);
            throw e;
        }
    },


    encryptV3: async function (input, password) {
        try {
            const iterations = 400000;
            // Generate nonce (12 bytes) and salt (16 bytes)
            const nonce = window.crypto.getRandomValues(new Uint8Array(12));
            const salt = window.crypto.getRandomValues(new Uint8Array(16));

            // Derive key (Reusing logic for better performance can be done, but keep safe first)
            const baseKey = await window.crypto.subtle.importKey(
                'raw',
                password,
                { name: 'PBKDF2' },
                false,
                ['deriveKey']
            );

            const deriveParams = {
                name: 'PBKDF2',
                hash: 'SHA-512',
                iterations: iterations,
                salt: salt
            };

            const derivedKey = await window.crypto.subtle.deriveKey(
                deriveParams,
                baseKey,
                { name: 'AES-GCM', length: 256 },
                false,
                ['encrypt']
            );

            // Encrypt
            const encrypted = await window.crypto.subtle.encrypt(
                { name: 'AES-GCM', iv: nonce },
                derivedKey,
                input
            );

            // Pack result: nonce (12) + salt (16) + ciphertext
            const output = new Uint8Array(12 + 16 + encrypted.byteLength);
            output.set(nonce, 0);
            output.set(salt, 12);
            output.set(new Uint8Array(encrypted), 28); // 12 + 16

            return output;

        } catch (e) {
            console.error('Encrypt V3 Error:', e);
            throw e;
        }
    },

    decryptV3: async function (ciphertext, password) {
        try {
            const input = ciphertext;
            const iterations = 400000;

            if (input.length < 28) throw new Error('Invalid ciphertext length');

            const nonce = input.slice(0, 12);
            const salt = input.slice(12, 28);
            const encryptedData = input.slice(28);

            // Derive key
            const baseKey = await window.crypto.subtle.importKey(
                'raw',
                password,
                { name: 'PBKDF2' },
                false,
                ['deriveKey']
            );

            const deriveParams = {
                name: 'PBKDF2',
                hash: 'SHA-512',
                iterations: iterations,
                salt: salt
            };

            const derivedKey = await window.crypto.subtle.deriveKey(
                deriveParams,
                baseKey,
                { name: 'AES-GCM', length: 256 },
                false,
                ['decrypt']
            );

            // Decrypt
            const decrypted = await window.crypto.subtle.decrypt(
                { name: 'AES-GCM', iv: nonce },
                derivedKey,
                encryptedData
            );

            return new Uint8Array(decrypted);

        } catch (e) {
            console.error('Decrypt V3 Error:', e);
            throw e; // Let C# handle
        }
    },

    // Generates deterministic password (legacy V2)
    generatePasswordV2: async function (privatePart, publicPart, hkdfPurpose) {
        try {
             // The default purpose used in v2 generator is 'Password'
            const purpose = hkdfPurpose || 'Password';

            const purposeBytes = new TextEncoder().encode(purpose);
            const iterations = 400000;

            const baseKey = await window.crypto.subtle.importKey(
                'raw',
                privatePart,
                { name: 'PBKDF2' },
                false,
                ['deriveKey']
            );

            const deriveParams = {
                name: 'PBKDF2',
                hash: 'SHA-512',
                iterations: iterations,
                salt: publicPart
            };

            // 1. Derive AES-GCM 256 key bits (32 bytes)
             const aesDerivedKey = await window.crypto.subtle.deriveKey(
                deriveParams,
                baseKey,
                { name: 'AES-GCM', length: 256 },
                true, // exportable
                ['encrypt'] // Dummy usage
            );

            const rawDerivedBytes = await window.crypto.subtle.exportKey('raw', aesDerivedKey);

            // 2. Import as HMAC key
            const hmacKey = await window.crypto.subtle.importKey(
                'raw',
                rawDerivedBytes,
                { name: 'HMAC', hash: { name: 'SHA-512' } },
                false,
                ['sign']
            );

            // 3. Sign
            const signature = await window.crypto.subtle.sign(
                'HMAC',
                hmacKey,
                purposeBytes
            );

            return new Uint8Array(signature);

        } catch (e) {
             console.error('Generate Password V2 Error:', e);
             throw e;
        }
    },

    generateRandomBytes: function(count) {
         const array = new Uint8Array(count);
         window.crypto.getRandomValues(array);
         return array;
    }
};
