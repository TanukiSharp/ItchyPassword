export function arrayToString(array: ArrayBuffer): string {
    const decoder = new TextDecoder(/*'utf-8'*/);
    return decoder.decode(array);
};

export function copy(source: Uint8Array, sourceIndex: number, target: Uint8Array, targetIndex: number, length: number): void {
    for (let i: number = 0; i < length; i += 1) {
        target[i + targetIndex] = source[i + sourceIndex];
    }
}

function createHeaderedBuffer(buffer: ArrayBuffer): ArrayBuffer {
    if (buffer.byteLength > 0xFFFF) {
        throw new Error(`Buffer too large: ${buffer.byteLength} bytes`);
    }

    let length = buffer.byteLength;
    const headeredBuffer: Uint8Array = new Uint8Array(2 + buffer.byteLength);

    for (let i: number = 0; i < 2; i += 1) {
        headeredBuffer[i] = length % 256;
        length /= 256;
    }

    headeredBuffer.set(new Uint8Array(buffer), 2);

    return headeredBuffer.buffer;
}

function arrayBufferToUnsignedBigIntWithoutHeader(arrayBuffer: ArrayBuffer): bigint {
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

function arrayBufferToUnsignedBigInt(arrayBuffer: ArrayBuffer): bigint {
    arrayBuffer = createHeaderedBuffer(arrayBuffer);

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

    while (number > 0n) {
        const remainder: bigint = number % 256n;
        number /= 256n;

        const byteValue: number = Number(<any>BigInt.asUintN(8, remainder));

        result.push(byteValue);
    }

    let totalLength: number = result[0];
    if (result.length > 1) { // For case where original buffer is of length 1 and contains 0.
        totalLength += result[1] * 256;
    }

    // The varable 'result' contains 2 bytes of size header.
    const diff = totalLength - (result.length - 2);

    for (let i: number = 0; i < diff; i += 1) {
        result.push(0);
    }

    return new Uint8Array(result.slice(2)).buffer;
}

// This is a one way encoding in the sense that decoding is not always deterministic.
// This can be used to generate strings where decoding it doesn't matter.
export function toCustomBaseOneWay(bytes: ArrayBuffer, alphabet: string): string {
    const alphabetLength: bigint = BigInt(alphabet.length);

    let result: string = '';
    let number: bigint = arrayBufferToUnsignedBigIntWithoutHeader(bytes);

    while (number > 0n) {
        const remainder: bigint = number % alphabetLength;
        number /= alphabetLength;

        const index: number = <number><any>BigInt.asUintN(8, remainder);

        result += alphabet[index];
    }

    return result;
}

export function toCustomBase(bytes: ArrayBuffer, alphabet: string): string {
    const alphabetLength: bigint = BigInt(alphabet.length);

    let result: string = '';
    let number: bigint = arrayBufferToUnsignedBigInt(bytes);

    while (number > 0n) {
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
        (x: number) => ('00' + x.toString(16)).slice(-2)
    ).join('');
}
