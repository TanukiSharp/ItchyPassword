import { ICipher, getDerivedBytes } from '../crypto';
import { CancellationToken, ensureNotCancelled } from '../asyncUtils';

const encryptionKeyDerivationSalt: ArrayBuffer = new Uint8Array([ 0xf2, 0xcf, 0xef, 0x8e, 0x13, 0x40, 0x46, 0x49, 0x92, 0x2a, 0xde, 0x5c, 0xbc, 0x88, 0x38, 0xa8 ]).buffer;

export class CipherV1 implements ICipher {
    private iterations: number = 100000;

    public get version(): number {
        return 1;
    }

    public get description(): string {
        return 'PBKDF2 + AES-GCM';
    }

    async encrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
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

        const passwordKey: CryptoKey = await window.crypto.subtle.importKey(
            'raw',
            await getDerivedBytes(password, encryptionKeyDerivationSalt, this.iterations, cancellationToken),
            aesKeyAlgorithm,
            false,
            ['encrypt']
        );

        ensureNotCancelled(cancellationToken);

        const result: ArrayBuffer = await window.crypto.subtle.encrypt(aesGcmParams, passwordKey, input);

        ensureNotCancelled(cancellationToken);

        new Uint8Array(output).set(new Uint8Array(result), 12);

        return output;
    }

    async decrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
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

        const derivedKey: ArrayBuffer = await getDerivedBytes(password, encryptionKeyDerivationSalt, this.iterations, cancellationToken);

        ensureNotCancelled(cancellationToken);

        const passwordKey: CryptoKey = await window.crypto.subtle.importKey(
            'raw',
            derivedKey,
            aesKeyAlgorithm,
            false,
            ['decrypt']
        );

        ensureNotCancelled(cancellationToken);

        const result: ArrayBuffer = await window.crypto.subtle.decrypt(aesGcmParams, passwordKey, payload);

        ensureNotCancelled(cancellationToken);

        return result;
    }
}
