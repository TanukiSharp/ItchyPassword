import * as ui from '../ui';
import { PlainObject, objectDeepSort } from '../PlainObject';
import { IComponent } from './IComponent';
import * as serviceManager from '../services/serviceManger';
import { VaultService } from '../services/vaultService';

import { IVaultStorage } from '../storages/IVaultStorage';
import { GitHubPersonalAccessTokenVaultStorage } from '../storages/GitHubVaultStorage';
import { SecureLocalStorage } from '../storages/SecureLocalStorage';

const divStorageOutput: HTMLElement = ui.getElementById('divStorageOutput');

const txtPath: HTMLInputElement = ui.getElementById('txtPath') as HTMLInputElement;
const lblMatchingPath: HTMLElement = ui.getElementById('lblMatchingPath');

const txtParameters: HTMLInputElement = ui.getElementById('txtParameters') as HTMLInputElement;
const btnPushToVault: HTMLButtonElement = ui.getElementById('btnPushToVault') as HTMLButtonElement;
const txtCustomKeys: HTMLInputElement = ui.getElementById('txtCustomKeys') as HTMLInputElement;

let vaultStorage: IVaultStorage = new GitHubPersonalAccessTokenVaultStorage(new SecureLocalStorage());

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

function createMatchingPath(path: string, depth: number): string {
    let position = 0;

    for (let i = 0; i < depth; i += 1) {
        position = path.indexOf('/', position);
        if (position < 0) {
            position = path.length + 1;
            break;
        }
        position += 1;
    }

    return path.substr(0, position - 1);
}

function updateMatchingPath(): void {
    const vaultService: VaultService = serviceManager.getService('vault');

    const depth = vaultService.computeUserPathMatchDepth(txtPath.value);

    if (depth > 0) {
        const matchingPath = createMatchingPath(txtPath.value, depth);
        lblMatchingPath.innerText = matchingPath;
    } else {
        lblMatchingPath.innerText = '';
    }
}

function onPathTextInput() {
    updateMatchingPath();
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

    txtCustomKeys.style.setProperty('background', ui.ERROR_COLOR);
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

async function pushToVault(): Promise<void> {
    console.log('txtParameters.value:', txtParameters.value);

    // vaultStorage.setVaultContent(
}

export function clearOutputs(): void {
    _parameterKeys = undefined;
    _parameterPath = undefined;
    _reservedKeys = undefined;
    ui.clearText(txtParameters);
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
        ui.setupFeedbackButton(btnPushToVault, pushToVault);
        txtPath.addEventListener('input', onPathTextInput);
    }
}
