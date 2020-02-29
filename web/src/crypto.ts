import * as arrayUtils from './arrayUtils';

export const BASE62_ALPHABET: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

export interface IPasswordGenerator {
    readonly version: number;
    readonly description: string;
    generatePassword(privatePart: ArrayBuffer, publicPart: ArrayBuffer): Promise<ArrayBuffer>;
}

export interface ICipher {
    readonly version: number;
    readonly description: string;
    encrypt(input: ArrayBuffer, password: ArrayBuffer): Promise<ArrayBuffer>;
    decrypt(input: ArrayBuffer, password: ArrayBuffer): Promise<ArrayBuffer>;
}

export async function getDerivedBytes(password: ArrayBuffer, salt: ArrayBuffer): Promise<ArrayBuffer> {
    const baseKey: CryptoKey = await window.crypto.subtle.importKey(
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

    const result: CryptoKey = await window.crypto.subtle.deriveKey(
        algorithm,
        baseKey,
        derivedKeyType,
        true,
        ['encrypt']
    );

    const key: ArrayBuffer = await window.crypto.subtle.exportKey('raw', result);

    return key;
}

export function generateRandomBytes(byteCount: number = 64): ArrayBuffer {
    const array: Uint8Array = new Uint8Array(byteCount);
    return crypto.getRandomValues(array).buffer;
}

export function generateRandomString(byteCount: number = 64, alphabet: string = BASE62_ALPHABET): string {
    const array: ArrayBuffer = generateRandomBytes(byteCount);
    return arrayUtils.toCustomBaseOneWay(array, alphabet);
}
