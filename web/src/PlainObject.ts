export type PlainObject = { [key: string]: any };

export function isPlainObject(value: any): boolean {
    return value !== undefined &&
        value !== null &&
        value.hasOwnProperty('constructor') === false &&
        value.constructor.name === 'Object';
}

export function objectDeepSort(object: PlainObject): PlainObject {
    const output: PlainObject = {};

    for (const [key, value] of Object.entries(object).sort((a, b) => a[0].localeCompare(b[0]))) {
        output[key] = isPlainObject(value) ? objectDeepSort(value) : value;
    }

    return output;
}
