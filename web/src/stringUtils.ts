export function truncate(input: string, length: number): string {
    if (input.length <= length) {
        return input;
    }

    return input.substr(0, length);
}

export function stringToArray(str: string): Uint8Array {
    const encoder = new TextEncoder(/*'utf-8'*/);
    return encoder.encode(str);
}
