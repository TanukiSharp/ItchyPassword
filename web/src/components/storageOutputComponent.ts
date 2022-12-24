import * as ui from '../ui';
import * as stringUtils from '../stringUtils';
import { PlainObject, objectDeepSort } from '../PlainObject';
import { IComponent } from './IComponent';
import { rootComponent, RootComponent } from './rootComponent';
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

    const firstPath: string = separatorIndex >= 0 ? path.substring(0, separatorIndex) : path;
    const remainingPath: string | undefined = separatorIndex >= 0 ? path.substring(separatorIndex + 1) : undefined;

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

    return path.substring(0, position - 1);
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
        return null;
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
    if (_parameterKeys === undefined || _parameterPath === undefined) {
        return;
    }

    const chainInfo: IChainInfo = pathToObjectChain(`${txtPath.value}/${_parameterPath}`);
    const leaf: PlainObject = chainInfo.tail;

    for (const [key, value] of Object.entries(_parameterKeys)) {
        leaf[key] = value;
    }

    const customKeys: PlainObject | null = parseCustomKeys();

    updateCustomKeysDisplay(txtCustomKeys.value === '' || customKeys !== null);

    if (customKeys !== null) {
        leaf.customKeys = customKeys;
    }

    if (Object.keys(leaf).length === 0) {
        // Remove the leaf object.
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = null;
    }

    txtParameters.value = JSON.stringify(objectDeepSort(chainInfo.head), undefined, 4);
}

function deepMerge(source: PlainObject, target: PlainObject): void {
    for (const sourceKey of Object.keys(source)) {
        const targetValue: any = target[sourceKey];
        const sourceValue: any = source[sourceKey];

        if (targetValue === undefined ||
            targetValue === null ||
            targetValue.constructor.name !== 'Object' ||
            sourceValue.constructor.name !== 'Object') {
            target[sourceKey] = sourceValue;
            continue;
        }

        deepMerge(sourceValue, targetValue);
    }
}

function generateUpdateMessage() {
    const activeComponent: IComponent | null = (rootComponent as RootComponent).getActiveComponent();

    if (activeComponent === null) {
        throw new Error('Could not determine active component.');
    }

    let hint: string = activeComponent.getVaultHint();

    const matchingPath: string = lblMatchingPath.innerText;
    const fullPath: string = txtPath.value;

    if (!matchingPath) {
        return `Added ${hint} for '${fullPath}'`;
    }

    if (matchingPath === fullPath) {
        return `Updated ${hint} for '${fullPath}'`;
    }

    const remainingPath: string = stringUtils.trim(fullPath.substring(matchingPath.length), '/');

    return `Updated ${hint} for '${matchingPath}' adding '${remainingPath}'`;
}

async function pushToVault(): Promise<boolean> {
    const vaultContentData: string | null = await vaultStorage.getVaultContent();

    if (vaultContentData === null) {
        return false;
    }

    const newData = JSON.parse(txtParameters.value);
    let vaultContent = JSON.parse(vaultContentData);

    // Keep deepMerge despite now all properties are explicitly defined, because of
    // the datetime property that really need to be overwritten only if it exists.
    deepMerge(newData, vaultContent);

    const message: string = generateUpdateMessage();

    const newVaultContentData: string = JSON.stringify(objectDeepSort(vaultContent), undefined, 4) + '\n';

    await vaultStorage.setVaultContent(newVaultContentData, `[ItchyPassword] ${message}`);

    return true;
}

export function clearOutputs(): void {
    _parameterKeys = undefined;
    _parameterPath = undefined;
    ui.clearText(txtParameters);
}

let _parameterKeys: PlainObject | undefined;
let _parameterPath: string | undefined;

export function setParameters(parameterKeys: PlainObject, parameterPath: string) {
    _parameterKeys = parameterKeys;
    _parameterPath = parameterPath;
    update();
}

export function setPathUI(path: string) {
    txtPath.value = path;
    onPathTextInput();
}

export function setCustomKeysUI(customKeys: string) {
    txtCustomKeys.value = customKeys;
}

export function show(): void {
    divStorageOutput.style.setProperty('display', 'initial');
}

export function hide(): void {
    divStorageOutput.style.setProperty('display', 'none');
}

export function clearUI(): void {
    txtPath.value = '';
    txtParameters.value = '';
    txtCustomKeys.value = '';
    _parameterKeys = undefined;
    _parameterPath = undefined;
    updateCustomKeysDisplay(true);
}

export function clearMatchingPath(): void {
    lblMatchingPath.innerText = '';
}

export class StorageOutputComponent implements IComponent {
    public readonly name: string = 'StorageOutput';

    public getVaultHint(): string {
        throw new Error('Not supported.');
    }

    public init(): void {
        txtCustomKeys.addEventListener('input', onCustomKeysTextInput);
        ui.setupFeedbackButton(btnPushToVault, pushToVault);
        txtPath.addEventListener('input', onPathTextInput);
    }
}
