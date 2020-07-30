function findNonCharFromStart(value: string, chars: string[]): number {
    for (let i = 0; i < value.length; i += 1) {
        if (chars.includes(value[i]) === false) {
            return i;
        }
    }

    return value.length;
}

function findNonCharFromEnd(value: string, chars: string[]): number {
    for (let i = value.length - 1; i >= 0; i -= 1) {
        if (chars.includes(value[i]) === false) {
            return i + 1;
        }
    }

    return value.length;
}

export function trim(value: string, ...chars: string[]): string {
    const start: number = findNonCharFromStart(value, chars);
    const end: number = findNonCharFromEnd(value, chars);

    return value.substring(start, end);
}

export function truncate(input: string, length: number): string {
    if (input.length <= length) {
        return input;
    }

    return input.substr(0, length);
}

export function stringToArray(str: string): ArrayBuffer {
    const encoder = new TextEncoder(/*'utf-8'*/);
    return encoder.encode(str).buffer;
}

export function fromBase16(str: string): ArrayBuffer {
    if (str.length % 2 !== 0) {
        str = '0' + str;
    }

    const result: Uint8Array = new Uint8Array(str.length / 2);

    for (let i = 0; i < result.byteLength; i += 1) {
        result[i] = parseInt(str.substr(i * 2, 2), 16);
    }

    return result.buffer;
}
