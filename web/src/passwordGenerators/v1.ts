import { stringToArray } from '../stringUtils';
import { IPasswordGenerator, getDerivedBytes } from '../crypto';
import { CancellationToken, ensureNotCancelled } from '../asyncUtils';

export class PasswordGeneratorV1 implements IPasswordGenerator {
    private hkdfPurpose: ArrayBuffer;
    private _description: string;

    public constructor(hkdfPurpose: string) {
        this.hkdfPurpose = stringToArray(hkdfPurpose);
        this._description = `HKDF(PBKDF2, HMAC512) [purpose: ${hkdfPurpose}]`;
    }

    public get version(): number {
        return 1;
    }

    public get description(): string {
        return this._description;
    }

    public async generatePassword(privatePart: ArrayBuffer, publicPart: ArrayBuffer, cancellationToken: CancellationToken): Promise<ArrayBuffer> {
        const derivedKey: ArrayBuffer = await getDerivedBytes(privatePart, publicPart, 100_000, cancellationToken);

        ensureNotCancelled(cancellationToken);

        const hmacParameters: HmacImportParams = {
            name: 'HMAC',
            hash: { name: 'SHA-512' }
        };

        const hkdfKey: CryptoKey = await window.crypto.subtle.importKey(
            'raw',
            derivedKey,
            hmacParameters,
            false,
            ['sign']
        );

        ensureNotCancelled(cancellationToken);

        const result: ArrayBuffer = await window.crypto.subtle.sign('HMAC', hkdfKey, this.hkdfPurpose);

        ensureNotCancelled(cancellationToken);

        return result;
    }
}
