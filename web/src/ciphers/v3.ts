import { ICipher, getDerivedBytes } from '../crypto';
import { CancellationToken, ensureNotCancelled } from '../asyncUtils';

// TODO: Should refactor v2 and v3 common code into a reusable component instead of shamelessly copy/pasting...

export class CipherV3 implements ICipher {
    private iterations: number = 400_000;

    public get version(): number {
        return 3;
    }

    public get description(): string {
        return 'PBKDF2 + AES-GCM 512';
    }

    async encrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
        const output: ArrayBuffer = new ArrayBuffer(12 + 16 + 16 + input.byteLength);

        const nonce: Uint8Array = crypto.getRandomValues(new Uint8Array(output, 0, 12));
        const passwordSalt: Uint8Array = crypto.getRandomValues(new Uint8Array(output, 12, 16));

        const aesGcmParams: AesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };

        const aesKeyAlgorithm: AesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 512
        };

        const passwordKey: CryptoKey = await window.crypto.subtle.importKey(
            'raw',
            await getDerivedBytes(password, passwordSalt, this.iterations, cancellationToken),
            aesKeyAlgorithm,
            false,
            ['encrypt']
        );

        ensureNotCancelled(cancellationToken);

        const result: ArrayBuffer = await window.crypto.subtle.encrypt(aesGcmParams, passwordKey, input);

        ensureNotCancelled(cancellationToken);

        new Uint8Array(output).set(new Uint8Array(result), 12 + 16);

        return output;
    }

    async decrypt(input: ArrayBuffer, password: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
        const nonce: Uint8Array = new Uint8Array(input, 0, 12);
        const passwordSalt: Uint8Array = new Uint8Array(input, 12, 16);
        const payload: Uint8Array = new Uint8Array(input, 12 + 16);

        const aesGcmParams: AesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };

        const aesKeyAlgorithm: AesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 512
        };

        const derivedKey: ArrayBuffer = await getDerivedBytes(password, passwordSalt, this.iterations, cancellationToken);

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
