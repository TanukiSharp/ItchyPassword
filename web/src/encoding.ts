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
        return `Alphabet in ASCII table order.`;
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

export class Base64Encoding implements IEncoding {
    get name(): string {
        return 'base64';
    }

    get description(): string {
        return '';
    }

    encode(input: ArrayBuffer): string {
        return this._utf8ArrToStr(new Uint8Array(input))
    }

    decode(input: string): ArrayBuffer {
        return this._strToUTF8Arr(input).buffer;
    }

    // _b64ToUint6(nChr: number): number {
    //     return nChr > 64 && nChr < 91
    //         ? nChr - 65
    //         : nChr > 96 && nChr < 123
    //             ? nChr - 71
    //             : nChr > 47 && nChr < 58
    //                 ? nChr + 4
    //                 : nChr === 43
    //                     ? 62
    //                     : nChr === 47
    //                         ? 63
    //                         : 0;
    // }

    // _base64DecToArr(sBase64: string, nBlocksSize?: number): Uint8Array {
    //     const sB64Enc = sBase64.replace(/[^A-Za-z0-9+/]/g, ''); // Remove any non-base64 characters, such as trailing '=', whitespace, and more.
    //     const nInLen = sB64Enc.length;
    //     const nOutLen = nBlocksSize
    //         ? Math.ceil(((nInLen * 3 + 1) >> 2) / nBlocksSize) * nBlocksSize
    //         : (nInLen * 3 + 1) >> 2;
    //     const taBytes = new Uint8Array(nOutLen);

    //     let nMod3;
    //     let nMod4;
    //     let nUint24 = 0;
    //     let nOutIdx = 0;
    //     for (let nInIdx = 0; nInIdx < nInLen; nInIdx++) {
    //         nMod4 = nInIdx & 3;
    //         nUint24 |= this._b64ToUint6(sB64Enc.charCodeAt(nInIdx)) << (6 * (3 - nMod4));
    //         if (nMod4 === 3 || nInLen - nInIdx === 1) {
    //             nMod3 = 0;
    //             while (nMod3 < 3 && nOutIdx < nOutLen) {
    //                 taBytes[nOutIdx] = (nUint24 >>> ((16 >>> nMod3) & 24)) & 255;
    //                 nMod3++;
    //                 nOutIdx++;
    //             }
    //             nUint24 = 0;
    //         }
    //     }

    //     return taBytes;
    // }

    // /* Base64 string to array encoding */
    // _uint6ToB64(nUint6: number): number {
    //     return nUint6 < 26
    //         ? nUint6 + 65
    //         : nUint6 < 52
    //             ? nUint6 + 71
    //             : nUint6 < 62
    //                 ? nUint6 - 4
    //                 : nUint6 === 62
    //                     ? 43
    //                     : nUint6 === 63
    //                         ? 47
    //                         : 65;
    // }

    // _base64EncArr(aBytes: Uint8Array): string {
    //     let nMod3 = 2;
    //     let sB64Enc = '';

    //     const nLen = aBytes.length;
    //     let nUint24 = 0;
    //     for (let nIdx = 0; nIdx < nLen; nIdx++) {
    //         nMod3 = nIdx % 3;
    //         // To break your base64 into several 80-character lines, add:
    //         //   if (nIdx > 0 && ((nIdx * 4) / 3) % 76 === 0) {
    //         //      sB64Enc += '\r\n';
    //         //    }

    //         nUint24 |= aBytes[nIdx] << ((16 >>> nMod3) & 24);
    //         if (nMod3 === 2 || aBytes.length - nIdx === 1) {
    //             sB64Enc += String.fromCodePoint(
    //                 this._uint6ToB64((nUint24 >>> 18) & 63),
    //                 this._uint6ToB64((nUint24 >>> 12) & 63),
    //                 this._uint6ToB64((nUint24 >>> 6) & 63),
    //                 this._uint6ToB64(nUint24 & 63)
    //             );
    //             nUint24 = 0;
    //         }
    //     }
    //     return (
    //         sB64Enc.substring(0, sB64Enc.length - 2 + nMod3) +
    //         (nMod3 === 2 ? '' : nMod3 === 1 ? '=' : '==')
    //     );
    // }

    /* UTF-8 array to JS string and vice versa */

    _utf8ArrToStr(aBytes: Uint8Array): string {
        let sView = '';
        let nPart;
        const nLen = aBytes.length;
        for (let nIdx = 0; nIdx < nLen; nIdx++) {
            nPart = aBytes[nIdx];
            sView += String.fromCodePoint(
                nPart > 251 && nPart < 254 && nIdx + 5 < nLen /* six bytes */
                    ? /* (nPart - 252 << 30) may be not so safe in ECMAScript! So…: */
                    (nPart - 252) * 1073741824 +
                    ((aBytes[++nIdx] - 128) << 24) +
                    ((aBytes[++nIdx] - 128) << 18) +
                    ((aBytes[++nIdx] - 128) << 12) +
                    ((aBytes[++nIdx] - 128) << 6) +
                    aBytes[++nIdx] -
                    128
                    : nPart > 247 && nPart < 252 && nIdx + 4 < nLen /* five bytes */
                        ? ((nPart - 248) << 24) +
                        ((aBytes[++nIdx] - 128) << 18) +
                        ((aBytes[++nIdx] - 128) << 12) +
                        ((aBytes[++nIdx] - 128) << 6) +
                        aBytes[++nIdx] -
                        128
                        : nPart > 239 && nPart < 248 && nIdx + 3 < nLen /* four bytes */
                            ? ((nPart - 240) << 18) +
                            ((aBytes[++nIdx] - 128) << 12) +
                            ((aBytes[++nIdx] - 128) << 6) +
                            aBytes[++nIdx] -
                            128
                            : nPart > 223 && nPart < 240 && nIdx + 2 < nLen /* three bytes */
                                ? ((nPart - 224) << 12) +
                                ((aBytes[++nIdx] - 128) << 6) +
                                aBytes[++nIdx] -
                                128
                                : nPart > 191 && nPart < 224 && nIdx + 1 < nLen /* two bytes */
                                    ? ((nPart - 192) << 6) + aBytes[++nIdx] - 128
                                    : /* nPart < 127 ? */ /* one byte */
                                    nPart
            );
        }
        return sView;
    }

    _strToUTF8Arr(sDOMStr: string) {
        let aBytes;
        let nChr: number | undefined;
        const nStrLen = sDOMStr.length;
        let nArrLen = 0;

        /* mapping… */
        for (let nMapIdx = 0; nMapIdx < nStrLen; nMapIdx++) {
            nChr = sDOMStr.codePointAt(nMapIdx);

            if (nChr === undefined) {
                throw new Error(`Invalid index nMapIdx ${nMapIdx} in sDOMStr '${sDOMStr}'.`);
            }

            if (nChr >= 0x10000) {
                nMapIdx++;
            }

            nArrLen +=
                nChr < 0x80
                    ? 1
                    : nChr < 0x800
                        ? 2
                        : nChr < 0x10000
                            ? 3
                            : nChr < 0x200000
                                ? 4
                                : nChr < 0x4000000
                                    ? 5
                                    : 6;
        }

        aBytes = new Uint8Array(nArrLen);

        /* transcription… */
        let nIdx = 0;
        let nChrIdx = 0;
        while (nIdx < nArrLen) {
            nChr = sDOMStr.codePointAt(nChrIdx);

            if (nChr === undefined) {
                throw new Error(`Invalid index nMapIdx ${nChrIdx} in sDOMStr '${sDOMStr}'.`);
            }

            if (nChr < 128) {
                /* one byte */
                aBytes[nIdx++] = nChr;
            } else if (nChr < 0x800) {
                /* two bytes */
                aBytes[nIdx++] = 192 + (nChr >>> 6);
                aBytes[nIdx++] = 128 + (nChr & 63);
            } else if (nChr < 0x10000) {
                /* three bytes */
                aBytes[nIdx++] = 224 + (nChr >>> 12);
                aBytes[nIdx++] = 128 + ((nChr >>> 6) & 63);
                aBytes[nIdx++] = 128 + (nChr & 63);
            } else if (nChr < 0x200000) {
                /* four bytes */
                aBytes[nIdx++] = 240 + (nChr >>> 18);
                aBytes[nIdx++] = 128 + ((nChr >>> 12) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 6) & 63);
                aBytes[nIdx++] = 128 + (nChr & 63);
                nChrIdx++;
            } else if (nChr < 0x4000000) {
                /* five bytes */
                aBytes[nIdx++] = 248 + (nChr >>> 24);
                aBytes[nIdx++] = 128 + ((nChr >>> 18) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 12) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 6) & 63);
                aBytes[nIdx++] = 128 + (nChr & 63);
                nChrIdx++;
            } /* if (nChr <= 0x7fffffff) */ else {
                /* six bytes */
                aBytes[nIdx++] = 252 + (nChr >>> 30);
                aBytes[nIdx++] = 128 + ((nChr >>> 24) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 18) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 12) & 63);
                aBytes[nIdx++] = 128 + ((nChr >>> 6) & 63);
                aBytes[nIdx++] = 128 + (nChr & 63);
                nChrIdx++;
            }
            nChrIdx++;
        }

        return aBytes;
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