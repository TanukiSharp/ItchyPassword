import * as arrayUtils from './arrayUtils';

export interface IEncoding {
    readonly name: string;
    readonly description: string;
    encode(input: ArrayBuffer): string;
    decode(input: string): ArrayBuffer;
}

const BASE58_ALPHABET: string = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';

export class Base58Encoding implements IEncoding {
    get name(): string {
        return 'base58';
    }

    get description(): string {
        return `Base58 alphabet in ASCII table order.`;
    }

    encode(input: ArrayBuffer): string {
        return arrayUtils.toCustomBaseFast(input, BASE58_ALPHABET);
    }

    decode(input: string): ArrayBuffer {
        return arrayUtils.fromCustomBaseFast(input, BASE58_ALPHABET);
    }
}

export const BASE62_ALPHABET: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

export class Base62Encoding implements IEncoding {
    get name(): string {
        return 'base62';
    }

    get description(): string {
        return `Alphabet '${BASE62_ALPHABET}'.`;
    }

    encode(input: ArrayBuffer): string {
        return arrayUtils.toCustomBase(input, BASE62_ALPHABET);
    }

    decode(input: string): ArrayBuffer {
        return arrayUtils.fromCustomBase(input, BASE62_ALPHABET);
    }
}

// Code to encode and decode base64 is taken from MDN and adjusted.

export class Base64Encoding implements IEncoding {
    get name(): string {
        return 'base64';
    }

    get description(): string {
        return '';
    }

    encode(input: ArrayBuffer): string {
        return this._base64Encode(new Uint8Array(input))
    }

    decode(input: string): ArrayBuffer {
        return this._base64Decode(input).buffer;
    }

    _base64ToUint6(base64Char: number): number {
        return base64Char > 64 && base64Char < 91
            ? base64Char - 65
            : base64Char > 96 && base64Char < 123
                ? base64Char - 71
                : base64Char > 47 && base64Char < 58
                    ? base64Char + 4
                    : base64Char === 43
                        ? 62
                        : base64Char === 47
                            ? 63
                            : 0;
    }

    _base64Decode(base64Input: string, blocksSize?: number): Uint8Array {
        // Remove any non-base64 characters, such as trailing '=', whitespace, and more.
        base64Input = base64Input.replace(/[^A-Za-z0-9+/]/g, '');

        const inputLength = base64Input.length;
        const outputLength = blocksSize
            ? Math.ceil(((inputLength * 3 + 1) >> 2) / blocksSize) * blocksSize
            : (inputLength * 3 + 1) >> 2;

        const outputBytes = new Uint8Array(outputLength);

        let value24Bits = 0;

        let outputIndex = 0;

        for (let inputIndex = 0; inputIndex < inputLength; inputIndex++) {
            const inputIndexMod4 = inputIndex % 4;

            value24Bits |= this._base64ToUint6(base64Input.charCodeAt(inputIndex)) << (6 * (3 - inputIndexMod4));

            if (inputIndexMod4 === 3 || inputLength - inputIndex === 1) {
                let inputIndexMod3 = 0;

                while (inputIndexMod3 < 3 && outputIndex < outputLength) {
                    outputBytes[outputIndex] = (value24Bits >>> ((0b1_0000 >>> inputIndexMod3) & 0b1_1000)) & 0xFF;
                    inputIndexMod3++;
                    outputIndex++;
                }

                value24Bits = 0;
            }
        }

        return outputBytes;
    }

    /* Base64 string to array encoding */
    _uint6ToBase64(value6Bits: number): number {
        return value6Bits < 26
            ? value6Bits + 65
            : value6Bits < 52
                ? value6Bits + 71
                : value6Bits < 62
                    ? value6Bits - 4
                    : value6Bits === 62
                        ? 43
                        : value6Bits === 63
                            ? 47
                            : 65;
    }

    _base64Encode(inputBytes: Uint8Array): string {
        let inputIndexMod3 = 2;
        let base64Output = '';

        const inputBytesLength = inputBytes.length;

        let value24Bits = 0;

        for (let inputIndex = 0; inputIndex < inputBytesLength; inputIndex++) {
            inputIndexMod3 = inputIndex % 3;

            // To break your base64 into several 80-character lines, add:
            // if (inputIndex > 0 && ((inputIndex * 4) / 3) % 76 === 0) {
            //     base64Output += '\n';
            // }

            const shift = 0b1_0000 >>> inputIndexMod3;
            const constrainedShift = shift & 0b1_1000;

            value24Bits |= inputBytes[inputIndex] << constrainedShift;

            if (inputIndexMod3 === 2 || inputBytes.length - inputIndex === 1) {
                base64Output += String.fromCodePoint(
                    this._uint6ToBase64((value24Bits >>> 18) & 63),
                    this._uint6ToBase64((value24Bits >>> 12) & 63),
                    this._uint6ToBase64((value24Bits >>> 6) & 63),
                    this._uint6ToBase64(value24Bits & 63)
                );
                value24Bits = 0;
            }
        }

        const padding = inputIndexMod3 === 2
            ? ''
            : inputIndexMod3 === 1
                ? '='
                : '==';

        return base64Output.substring(0, base64Output.length - 2 + inputIndexMod3) + padding;
    }
}

// ---------------------------------------------------------------

export const availableEncodings: IEncoding[] = [
    new Base62Encoding(),
    new Base58Encoding(),
    new Base64Encoding(),
];

export function findEncodingByName(name: string): IEncoding | null {
    for (const encoding of availableEncodings) {
        if (encoding.name === name) {
            return encoding;
        }
    }

    return null;
}
