export function arrayToString(array: ArrayBuffer): string {
    const decoder = new TextDecoder(/*'utf-8'*/);
    return decoder.decode(array);
};

export function copy(source: Uint8Array, sourceIndex: number, target: Uint8Array, targetIndex: number, length: number): void {
    for (let i: number = 0; i < length; i += 1) {
        target[i + targetIndex] = source[i + sourceIndex];
    }
}

export function arrayBufferToUnsignedBigInt(arrayBuffer: ArrayBuffer): bigint {
    const length: number = arrayBuffer.byteLength;
    const arrayView: DataView = new DataView(arrayBuffer, 0);

    let result: bigint = 0n;
    let multiplier: bigint = 1n;

    for (let i: number = 0; i < length; i += 1) {
        result += BigInt(arrayView.getUint8(i)) * multiplier;
        multiplier *= 256n;
    }

    return result;
}

export function unsignedBigIntToArrayBuffer(number: bigint): ArrayBuffer {
    const result: Array<number> = [];

    while (number > 0n)
    {
        const remainder: bigint = number % 256n;
        number /= 256n;

        const byteValue: number = Number(<any>BigInt.asUintN(8, remainder));

        result.push(byteValue);
    }

    return new Uint8Array(result).buffer;
}

export function toCustomBase(bytes: ArrayBuffer, alphabet: string): string {
    const alphabetLength: bigint = BigInt(alphabet.length);

    let result: string = '';
    let number: bigint = arrayBufferToUnsignedBigInt(bytes);

    while (number > 0n)
    {
        const remainder: bigint = number % alphabetLength;
        number /= alphabetLength;

        const index: number = <number><any>BigInt.asUintN(8, remainder);

        result += alphabet[index];
    }

    return result;
}

export function fromCustomBase(input: string, alphabet: string): ArrayBuffer {
    const alphabetLength: bigint = BigInt(alphabet.length);

    let number: bigint = 0n;
    let multiplier: bigint = 1n;

    for (let i: number = 0; i < input.length; i += 1) {
        const value: bigint = BigInt(alphabet.indexOf(input[i]));

        number += value * multiplier;
        multiplier *= alphabetLength;
    }

    return unsignedBigIntToArrayBuffer(number);
}

export function toBase16(buffer: ArrayBuffer): string {
    return Array.prototype.map.call(
        new Uint8Array(buffer),
        x => ('00' + x.toString(16)).slice(-2)
    ).join('');
}
