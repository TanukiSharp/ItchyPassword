const algorithmName = 'PBKDF2';
const defaultCustomAlphabet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';

const truncate = (input, length) => {
    if (input.length <= length) {
        return input;
    }

    return input.substr(0, length);
};

const copy = (source, sourceIndex, target, targetIndex, length) => {
    for (let i = 0; i < length; i += 1) {
        target[i + targetIndex] = source[i + sourceIndex];
    }
};

const ensureRepeatedTo8Bytes = (salt) => {
    if (salt.length <= 0 || salt.length >= 8)
        return salt;

    const result = new Uint8Array(8);

    for (let i = 0; i < result.length; i += salt.length) {
        copy(salt, 0, result, i, Math.min(salt.length, result.length - i));
    }

    return result;
}

const arrayBufferToUnsignedBigInt = (arrayBuffer) => {
    const length = arrayBuffer.byteLength;
    const arrayView = new DataView(arrayBuffer, 0);

    let result = 0n;

    for (var i = 0; i < length; i += 1) {
        result += BigInt(arrayView.getUint8(i)) * (256n ** BigInt(i));
    }

    return result;
};

const toCustomBase = (bytes, alphabet) => {
    const alphabetLength = BigInt(alphabet.length);

    let result = '';
    let number = arrayBufferToUnsignedBigInt(bytes);

    while (number > 0n)
    {
        const remainder = number % alphabetLength;
        number /= alphabetLength;

        const index = BigInt.asUintN(64, remainder);

        result += alphabet[index];
    }

    return result;
}

const stringToArray = (str) => {
    const encoder = new TextEncoder('utf-8');
    return encoder.encode(str);
};

const bufferToHexadeximal = (buffer) => {
    return Array.prototype.map.call(
        new Uint8Array(buffer),
        x => ('00' + x.toString(16)).slice(-2)
    ).join('');
};

const bufferToBase64 = (buffer) => {
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buffer)));
}

const generatePassword = async (privateKey, publicKey) => {
    const algorithmSalt = ensureRepeatedTo8Bytes(stringToArray(publicKey));

    const algorithm = {
        name: algorithmName,
        hash: 'SHA-512',
        iterations: 100000,
        salt: algorithmSalt
    };

    const derivedKeyType = {
        name: 'AES-CBC',
        length: 256
    };

    const baseKey = await crypto.subtle.importKey(
        'raw',
        stringToArray(privateKey),
        algorithmName,
        false,
        ['deriveKey']
    );

    const result = await crypto.subtle.deriveKey(
        algorithm,
        baseKey,
        derivedKeyType,
        true,
        ['encrypt']
    );

    return await crypto.subtle.exportKey('raw', result);
};
