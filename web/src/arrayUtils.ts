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

    for (let i: number = 0; i < length; i += 1) {
        result += BigInt(arrayView.getUint8(i)) * (256n ** BigInt(i));
    }

    return result;
}

export function toCustomBase(bytes: ArrayBuffer, alphabet: string): string {
    const alphabetLength: bigint = BigInt(alphabet.length);

    let result: string = '';
    let number: bigint = arrayBufferToUnsignedBigInt(bytes);

    while (number > 0n)
    {
        const remainder: bigint = number % alphabetLength;
        number /= alphabetLength;

        const index: number = <number><any>BigInt.asUintN(64, remainder);

        result += alphabet[index];
    }

    return result;
}

export function toBase16(buffer: ArrayBuffer): string {
    return Array.prototype.map.call(
        new Uint8Array(buffer),
        x => ('00' + x.toString(16)).slice(-2)
    ).join('');
}
