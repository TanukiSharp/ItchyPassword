import * as stringUtils from './stringUtils';
import * as arrayUtils from './arrayUtils';

export const BASE62_ALPHABET: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

const encryptionKeyDerivationSalt: ArrayBuffer = new Uint8Array([ 0xf2, 0xcf, 0xef, 0x8e, 0x13, 0x40, 0x46, 0x49, 0x92, 0x2a, 0xde, 0x5c, 0xbc, 0x88, 0x38, 0xa8 ]).buffer;

async function getDerivedBytes(password: ArrayBuffer, salt: ArrayBuffer): Promise<ArrayBuffer> {
    const baseKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        password,
        'PBKDF2',
        false,
        ['deriveKey']
    );

    const algorithm: Pbkdf2Params = {
        name: 'PBKDF2',
        hash: 'SHA-512',
        iterations: 100000,
        salt
    };

    const derivedKeyType: AesDerivedKeyParams = {
        name: 'AES-CBC',
        length: 256
    };

    const result: CryptoKey = await crypto.subtle.deriveKey(
        algorithm,
        baseKey,
        derivedKeyType,
        true,
        ['encrypt']
    );

    const key: ArrayBuffer = await crypto.subtle.exportKey('raw', result);

    return key;
}

export async function generatePassword(privatePart: ArrayBuffer, publicPart: ArrayBuffer, hkdfPurpose: string): Promise<ArrayBuffer> {
    const derivedKey: ArrayBuffer = await getDerivedBytes(privatePart, publicPart);

    const hmacParameters: HmacImportParams = {
        name: 'HMAC',
        hash: { name: 'SHA-512' }
    };

    const hkdfKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        derivedKey,
        hmacParameters,
        false,
        ['sign']
    );

    return await crypto.subtle.sign('HMAC', hkdfKey, stringUtils.stringToArray(hkdfPurpose));
}

export function generateRandomBytes(byteCount: number = 64): ArrayBuffer {
    const array: Uint8Array = new Uint8Array(byteCount);
    return crypto.getRandomValues(array).buffer;
}

export function generateRandomString(byteCount: number = 64, alphabet: string = BASE62_ALPHABET): string {
    const array: ArrayBuffer = generateRandomBytes(byteCount);
    return arrayUtils.toCustomBase(array, alphabet);
}

export async function encrypt(input: ArrayBuffer, password: ArrayBuffer): Promise<ArrayBuffer> {
    const output: ArrayBuffer = new ArrayBuffer(12 + 16 + input.byteLength);

    const nonce: DataView = new DataView(output, 0, 12);
    crypto.getRandomValues(new Uint8Array(output, 0, 12));

    const aesGcmParams: AesGcmParams = {
        name: 'AES-GCM',
        iv: nonce
    };

    const aesKeyAlgorithm: AesKeyAlgorithm = {
        name: 'AES-GCM',
        length: 256
    };

    const passwordKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        await getDerivedBytes(password, encryptionKeyDerivationSalt),
        aesKeyAlgorithm,
        false,
        ['encrypt']
    );

    const result: ArrayBuffer = await crypto.subtle.encrypt(aesGcmParams, passwordKey, input);

    new Uint8Array(output).set(new Uint8Array(result), 12);

    return output;
}

export async function decrypt(input: ArrayBuffer, password: ArrayBuffer): Promise<ArrayBuffer> {
    const nonce: DataView = new DataView(input, 0, 12);
    const payload: DataView = new DataView(input, 12);

    const aesGcmParams: AesGcmParams = {
        name: 'AES-GCM',
        iv: nonce
    };

    const aesKeyAlgorithm: AesKeyAlgorithm = {
        name: 'AES-GCM',
        length: 256
    };

    const derivedKey: ArrayBuffer = await getDerivedBytes(password, encryptionKeyDerivationSalt);

    const passwordKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        derivedKey,
        aesKeyAlgorithm,
        false,
        ['decrypt']
    );

    return await crypto.subtle.decrypt(aesGcmParams, passwordKey, payload);
}
