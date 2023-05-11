import * as cipherComponent from '../components/cipherComponent';
import { CancellationToken } from '../asyncUtils';
import { Base58Encoding, IEncoding } from '../encoding';

const encoding: IEncoding = new Base58Encoding();

export interface IAsyncStorage {
    readonly length: number;
    clear(): void;
    getItem(key: string): Promise<string | null>;
    key(index: number): string | null;
    removeItem(key: string): void;
    setItem(key: string, value: string): Promise<void>;
}

export class SecureLocalStorage implements IAsyncStorage {
    get length(): number {
        return window.localStorage.length;
    }

    clear(): void {
        window.localStorage.clear();
    }

    key(index: number): string | null {
        return window.localStorage.key(index);
    }

    removeItem(key: string): void {
        window.localStorage.removeItem(key);
    }

    async getItem(key: string): Promise<string | null> {
        const encryptedItem: string | null = window.localStorage.getItem(key);

        if (encryptedItem === null) {
            return null;
        }

        const sortedCiphers = cipherComponent.ciphers.map(x => x).sort((a, b) => b.version - a.version);

        for (const cipher of sortedCiphers) {
            const result: string | null = await cipherComponent.decryptStringWithCipher(encryptedItem, cipher, encoding, CancellationToken.none);
            if (result !== null) {
                return result;
            }
        }

        return null;
    }

    async setItem(key: string, value: string): Promise<void> {
        const encrypted: string | null = await cipherComponent.encryptString(value, encoding, CancellationToken.none);

        if (encrypted === null) {
            console.error('Failed to encrypt value. (nothing stored)');
            return;
        }

        window.localStorage.setItem(key, encrypted);
    }
}
