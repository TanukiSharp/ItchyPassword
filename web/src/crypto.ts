import * as stringUtils from './stringUtils';
import * as arrayUtils from './arrayUtils';

const ALGORITHM_NAME: string = 'PBKDF2';
const SHA_ALGORITHM_NAME = 'SHA-512';
export const BASE62_ALPHABET: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

export async function generatePassword(privateKey: string, publicKey: string, hkdfPurpose: string): Promise<ArrayBuffer> {
    const algorithmSalt: Uint8Array = stringUtils.stringToArray(publicKey);
    const hkdfPurposeBytes: Uint8Array = stringUtils.stringToArray(hkdfPurpose);

    if (algorithmSalt.length < 8) {
        throw new Error('Public key must be at least 8 bytes long.');
    }

    const baseKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        stringUtils.stringToArray(privateKey),
        ALGORITHM_NAME,
        false,
        ['deriveKey']
    );

    const algorithm: Pbkdf2Params = {
        name: ALGORITHM_NAME,
        hash: SHA_ALGORITHM_NAME,
        iterations: 100000,
        salt: algorithmSalt
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

    const hmacParameters: HmacImportParams = {
        name: 'HMAC',
        hash: { name: SHA_ALGORITHM_NAME }
    };

    const hkdfKey: CryptoKey = await crypto.subtle.importKey(
        'raw',
        key,
        hmacParameters,
        false,
        ['sign']
    );

    return await crypto.subtle.sign('HMAC', hkdfKey, hkdfPurposeBytes);
}

export function generateRandomString(byteCount: number = 64, alphabet: string = BASE62_ALPHABET): string {
    const array: Uint8Array = new Uint8Array(byteCount);
    crypto.getRandomValues(array);
    return arrayUtils.toCustomBase(array.buffer, BASE62_ALPHABET);
}
