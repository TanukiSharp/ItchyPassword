import { getElementById, ERROR_COLOR } from '../ui';
import { PlainObject } from '../PlainObject';
import { IComponent } from './IComponent';

const divStorageOutput: HTMLInputElement = getElementById('divStorageOutput');

const txtPath: HTMLInputElement = getElementById('txtPath');

const txtParameters: HTMLInputElement = getElementById('txtParameters');
const txtCustomKeys: HTMLInputElement = getElementById('txtCustomKeys');

function isPlainObject(value: any): boolean {
    return value !== undefined && value !== null && value.constructor.name === 'Object';
}

function objectDeepSort(object: PlainObject): PlainObject {
    const output: PlainObject = {};

    for (const [key, value] of Object.entries(object).sort((a, b) => a[0].localeCompare(b[0]))) {
        output[key] = isPlainObject(value) ? objectDeepSort(value) : value;
    }

    return output;
}

function shallowMerge(source: PlainObject | null, target: PlainObject | null, reservedKeys: string[]): PlainObject {
    const result: PlainObject = {};

    if (source !== null) {
        for (const [key, value] of Object.entries(source)) {
            if (reservedKeys.includes(key) === false) {
                result[key] = value;
            }
        }
    }

    if (target !== null) {
        for (const [key, value] of Object.entries(target)) {
            result[key] = value;
        }
    }

    return result;
}

type IChainInfo = {
    head: PlainObject,
    tailParent: PlainObject,
    tail: PlainObject
};

// Transforms a path like "a/b/c/d" into a hierarchy of objects like { "a": { "b": { "c": { "d": {} } } } }
// From the result object, head is the root object that contains "a", tail is the value of "d", and tailParent is the value of "c"
function pathToObjectChain(path: string, chainInfo: IChainInfo | undefined = undefined): IChainInfo {
    const separatorIndex: number = path.indexOf('/');

    const tail: PlainObject = {};

    const firstPath: string = separatorIndex >= 0 ? path.substr(0, separatorIndex) : path;
    const remainingPath: string | undefined = separatorIndex >= 0 ? path.substr(separatorIndex + 1) : undefined;

    if (chainInfo === undefined) {
        const node: PlainObject = {};
        node[firstPath] = tail;
        chainInfo = {
            head: node,
            tailParent: node,
            tail
        };
    } else {
        chainInfo.tail[firstPath] = tail;
        chainInfo.tailParent = chainInfo.tail;
        chainInfo.tail = tail;
    }

    if (remainingPath) {
        return pathToObjectChain(remainingPath, chainInfo);
    }

    return chainInfo;
}

function onPathTextInput() {
    update();
}

function onCustomKeysTextInput(): void {
    update();
}

function updateCustomKeysDisplay(isValid: boolean): void {
    if (isValid) {
        txtCustomKeys.style.removeProperty('background');
        return;
    }

    txtCustomKeys.style.setProperty('background', ERROR_COLOR);
}

function parseCustomKeys(): PlainObject | null {
    if (txtCustomKeys.value === '') {
        return {};
    }

    try {
        const obj: any = JSON.parse(txtCustomKeys.value);
        if (obj === null || obj.constructor.name !== 'Object') {
            return null;
        }
        return obj as PlainObject;
    } catch {
        return null;
    }
}

function update(): void {
    if (_parameterKeys === undefined || _parameterPath === undefined || _reservedKeys === undefined) {
        return;
    }

    const chainInfo: IChainInfo = pathToObjectChain(`${txtPath.value}/${_parameterPath}`);
    const leaf: PlainObject = chainInfo.tail;

    for (const [key, value] of Object.entries(_parameterKeys)) {
        leaf[key] = value;
    }

    const customKeys: PlainObject | null = parseCustomKeys();
    updateCustomKeysDisplay(customKeys !== null);
    const resultParameters: PlainObject = shallowMerge(customKeys, leaf, _reservedKeys);

    if (Object.keys(resultParameters).length === 0) {
        // Set the value of the first (single) property of the object to null.
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = null;
    } else {
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = resultParameters;
    }

    txtParameters.value = JSON.stringify(objectDeepSort(chainInfo.head), undefined, 4);
}

export function clearOutputs(): void {
    _parameterKeys = undefined;
    _parameterPath = undefined;
    _reservedKeys = undefined;
    txtParameters.value = '';
}

let _parameterKeys: PlainObject | undefined;
let _parameterPath: string | undefined;
let _reservedKeys: string[] | undefined;

export function setParameters(parameterKeys: PlainObject, parameterPath: string, reservedKeys: string[]) {
    _parameterKeys = parameterKeys;
    _parameterPath = parameterPath;
    _reservedKeys = reservedKeys;
    update();
}

export function show(): void {
    divStorageOutput.style.setProperty('display', 'initial');
}

export function hide(): void {
    divStorageOutput.style.setProperty('display', 'none');
}

export class StorageOutputComponent implements IComponent {
    init(): void {
        txtCustomKeys.addEventListener('input', onCustomKeysTextInput);
        txtPath.addEventListener('input', onPathTextInput);
    }
}
