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

const algorithmName = 'PBKDF2';

const generatePassword = async (privateKey, publicKey) => {
    const algorithmSalt = stringToArray(publicKey);

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

const postData = async (url = '', data = {}) => {
    const response = await fetch(url, {
        method: 'POST',
        cache: 'no-cache',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });

    return response.text();
};

const generateRandomString = (alphabet) => {
    const size = Math.random() * 8 + 24;

    let result = '';
    for (let i = 0; i < size; i += 1) {
        const index = Math.floor(Math.random() * alphabet.length);
        result += alphabet[index];
    }

    return result;
};

const validate = async () => {
    const alphabet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';

    for (let i = 0; i < 150; i += 1) {
        const privateKey = generateRandomString(alphabet);
        const publicKey = generateRandomString(alphabet);

        const localDerivedBytes = await generatePassword(privateKey, publicKey);
        const localDerivedKey = toCustomBase(localDerivedBytes, alphabet);

        const remoteDerivedKey = await postData('http://localhost:5000/', {
            privateKey,
            publicKey,
            iterations: 100000,
            algorithmName: "SHA512",
            alphabet
        });

        if (localDerivedKey !== remoteDerivedKey) {
            throw new Error(`Keys mismatch at test ${i}, local: ${localDerivedKey}, remote: ${remoteDerivedKey}`);
        }

        console.log(localDerivedKey);
    }
};

const main = async () => {
    console.log('Validating...');
    await validate();
    console.log('Validated');

    const keyBytes = await generatePassword('furet', 'en-mousse');

    const b16Key = bufferToHexadeximal(keyBytes);
    const b64Key = bufferToBase64(keyBytes);

    console.log(`b16: ${b16Key}`);
    console.log(`b64: ${b64Key}`);

    const alphabet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';
    console.log(`b${alphabet.length}: ${toCustomBase(keyBytes, alphabet)}`);
}

main();
