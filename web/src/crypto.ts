import * as stringUtils from './stringUtils';
import * as arrayUtils from './arrayUtils';

const ALGORITHM_NAME: string = 'PBKDF2';
export const BASE62_ALPHABET: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

export async function generatePassword(privateKey: string, publicKey: string): Promise<ArrayBuffer> {
    const algorithmSalt: Uint8Array = stringUtils.stringToArray(publicKey);

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
        hash: 'SHA-512',
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

    return await crypto.subtle.exportKey('raw', result);
}

export function generateRandomString(byteCount: number = 64, alphabet: string = BASE62_ALPHABET): string {
    const array: Uint8Array = new Uint8Array(byteCount);
    crypto.getRandomValues(array);
    return arrayUtils.toCustomBase(array.buffer, BASE62_ALPHABET);
}
