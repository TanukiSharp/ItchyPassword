const ALGORITHM_NAME = 'PBKDF2';
const BASE62_ALPHABET = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

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
    const algorithmSalt = stringToArray(publicKey);

    if (algorithmSalt.length < 8) {
        throw new Error('Public key must be at least 8 bytes long.');
    }

    const baseKey = await crypto.subtle.importKey(
        'raw',
        stringToArray(privateKey),
        ALGORITHM_NAME,
        false,
        ['deriveKey']
    );

    const algorithm = {
        name: ALGORITHM_NAME,
        hash: 'SHA-512',
        iterations: 100000,
        salt: algorithmSalt
    };

    const derivedKeyType = {
        name: 'AES-CBC',
        length: 256
    };

    const result = await crypto.subtle.deriveKey(
        algorithm,
        baseKey,
        derivedKeyType,
        true,
        ['encrypt']
    );

    return await crypto.subtle.exportKey('raw', result);
};

const generateRandomString = (byteCount = 64) => {
    const array = new Uint8Array(byteCount);
    crypto.getRandomValues(array);
    return toCustomBase(array.buffer, BASE62_ALPHABET);
};
