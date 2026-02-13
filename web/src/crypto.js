export async function generatePassword(privateKey, publicKey, iterations) {
    const key = await window.crypto.subtle.importKey(
        "raw",
        privateKey,
        { name: "PBKDF2" },
        false,
        ["deriveBits"]
    );

    const derivedBits = await window.crypto.subtle.deriveBits(
        {
            name: "PBKDF2",
            salt: publicKey,
            iterations: iterations,
            hash: "SHA-512"
        },
        key,
        256
    );

    const hmacKey = await window.crypto.subtle.importKey(
        "raw",
        derivedBits,
        { name: "HMAC", hash: { name: "SHA-512" } },
        false,
        ["sign"]
    );

    const encoder = new TextEncoder();
    const data = encoder.encode("Password");
    const signature = await window.crypto.subtle.sign(
        "HMAC",
        hmacKey,
        data
    );

    return new Uint8Array(signature);
}

export async function encrypt(data, key, iterations) {
    const salt = window.crypto.getRandomValues(new Uint8Array(16));
    const iv = window.crypto.getRandomValues(new Uint8Array(12));

    const importedKey = await window.crypto.subtle.importKey(
        "raw",
        key,
        { name: "PBKDF2" },
        false,
        ["deriveKey"]
    );

    const derivedKey = await window.crypto.subtle.deriveKey(
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

    const encryptedContent = await window.crypto.subtle.encrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        derivedKey,
        data
    );

    const result = new Uint8Array(iv.length + salt.length + encryptedContent.byteLength);
    result.set(iv, 0);
    result.set(salt, 12);
    result.set(new Uint8Array(encryptedContent), 28);
    return result;
}

export async function decrypt(data, key, iterations) {
    const iv = data.slice(0, 12);
    const salt = data.slice(12, 28);
    const ciphertext = data.slice(28);

    const importedKey = await window.crypto.subtle.importKey(
        "raw",
        key,
        { name: "PBKDF2" },
        false,
        ["deriveKey"]
    );

    const derivedKey = await window.crypto.subtle.deriveKey(
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

    const decrypted = await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        derivedKey,
        ciphertext
    );

    return new Uint8Array(decrypted);
}
