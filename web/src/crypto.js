export function generatePassword(privateKey, publicKey, iterations) {
    return window.crypto.subtle.importKey(
        "raw",
        privateKey,
        { name: "PBKDF2" },
        false,
        ["deriveBits"]
    ).then(function(key) {
        return window.crypto.subtle.deriveBits(
            {
                name: "PBKDF2",
                salt: publicKey,
                iterations: iterations,
                hash: "SHA-512"
            },
            key,
            256
        );
    }).then(function(derivedKey) {
        return window.crypto.subtle.importKey(
            "raw",
            derivedKey,
            { name: "HMAC", hash: { name: "SHA-512" } },
            false,
            ["sign"]
        );
    }).then(function(hmacKey) {
        var encoder = new TextEncoder();
        var data = encoder.encode("Password");
        return window.crypto.subtle.sign(
            "HMAC",
            hmacKey,
            data
        );
    }).then(function(signature) {
        return new Uint8Array(signature);
    });
}

export function encrypt(data, key, iterations) {
    var salt = window.crypto.getRandomValues(new Uint8Array(16));
    var iv = window.crypto.getRandomValues(new Uint8Array(12));

    return window.crypto.subtle.importKey(
        "raw",
        key,
        { name: "PBKDF2" },
        false,
        ["deriveKey"]
    ).then(function(importedKey) {
        return window.crypto.subtle.deriveKey(
            {
                name: "PBKDF2",
                salt: salt,
                iterations: iterations,
                hash: "SHA-512"
            },
            importedKey,
            { name: "AES-GCM", length: 256 },
            false,
            ["encrypt"]
        );
    }).then(function(derivedKey) {
        return window.crypto.subtle.encrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            derivedKey,
            data
        );
    }).then(function(encryptedContent) {
        var result = new Uint8Array(iv.length + salt.length + encryptedContent.byteLength);
        result.set(iv, 0);
        result.set(salt, 12);
        result.set(new Uint8Array(encryptedContent), 28);
        return result;
    });
}

export function decrypt(data, key, iterations) {
    var iv = data.slice(0, 12);
    var salt = data.slice(12, 28);
    var ciphertext = data.slice(28);

    return window.crypto.subtle.importKey(
        "raw",
        key,
        { name: "PBKDF2" },
        false,
        ["deriveKey"]
    ).then(function(importedKey) {
        return window.crypto.subtle.deriveKey(
            {
                name: "PBKDF2",
                salt: salt,
                iterations: iterations,
                hash: "SHA-512"
            },
            importedKey,
            { name: "AES-GCM", length: 256 },
            false,
            ["decrypt"]
        );
    }).then(function(derivedKey) {
        return window.crypto.subtle.decrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            derivedKey,
            ciphertext
        );
    }).then(function(decrypted) {
        return new Uint8Array(decrypted);
    });
}
