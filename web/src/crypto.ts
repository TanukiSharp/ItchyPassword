import { Base58Encoding, IEncoding } from './encoding';
import { CancellationToken, ensureNotCancelled } from './asyncUtils';

const encoding: IEncoding = new Base58Encoding();

export interface IPasswordGenerator {
    readonly version: number;
    readonly description: string;
    generatePassword(privatePart: ArrayBuffer, publicPart: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer>;
}

export interface ICipher {
    readonly version: number;
    readonly description: string;
    encrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer>;
    decrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer>;
}

export async function getDerivedBytes(password: ArrayBuffer, salt: ArrayBuffer, iterations: number, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
    const baseKey: CryptoKey = await window.crypto.subtle.importKey(
        'raw',
        password,
        'PBKDF2',
        false,
        ['deriveKey']
    );

    ensureNotCancelled(cancellationToken);

    const algorithm: Pbkdf2Params = {
        name: 'PBKDF2',
        hash: 'SHA-512',
        iterations: iterations,
        salt
    };

    const derivedKeyType: AesDerivedKeyParams = {
        // Algorithm name must be a recognized one,
        // but any AES-* produces the same result...
        name: 'AES-GCM',
        length: 256
    };

    const result: CryptoKey = await window.crypto.subtle.deriveKey(
        algorithm,
        baseKey,
        derivedKeyType,
        true,
        ['encrypt']
    );

    ensureNotCancelled(cancellationToken);

    const key: ArrayBuffer = await window.crypto.subtle.exportKey('raw', result);

    ensureNotCancelled(cancellationToken);

    return key;
}

export function generateRandomBytes(byteCount: number = 64): ArrayBuffer {
    const array: Uint8Array = new Uint8Array(byteCount);
    return crypto.getRandomValues(array).buffer;
}

export function generateRandomString(byteCount: number = 64): string {
    const array: ArrayBuffer = generateRandomBytes(byteCount);
    return encoding.encode(array);
}
