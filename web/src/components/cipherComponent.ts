import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import * as ui from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { IEncoding, availableEncodings, findEncodingByName } from '../encoding';

import { CipherV2 } from '../ciphers/v2';
import { CipherV3 } from '../ciphers/v3';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

import * as serviceManager from '../services/serviceManger';
import { CipherService } from '../services/cipherService';

import { CancellationToken, ensureNotCancelled, rethrowCancelled } from '../asyncUtils';
import { PlainObject } from '../PlainObject';

export const RECOMMENDED_ENCODING_NAME = 'base58';
export const LEGACY_ENCODING_NAME = 'base62';

const btnTabCiphers = ui.getElementById('btnTabCiphers') as HTMLButtonElement;
const divTabCiphers = ui.getElementById('divTabCiphers');

export const ciphers: crypto.ICipher[] = [
    new CipherV2(),
    new CipherV3(),
];

const btnClearAllCipherInfo = ui.getElementById('btnClearAllCipherInfo') as HTMLButtonElement;

const txtCipherName = ui.getElementById('txtCipherName') as HTMLInputElement;
const txtCipherSource = ui.getElementById('txtCipherSource') as HTMLInputElement;
const txtCipherTarget = ui.getElementById('txtCipherTarget') as HTMLInputElement;

const cboCipherVersion = ui.getElementById('cboCipherVersion') as HTMLSelectElement;
const cboCipherEncoding = ui.getElementById('cboCipherEncoding') as HTMLSelectElement;
const btnEncrypt = ui.getElementById('btnEncrypt') as HTMLButtonElement;
const btnDecrypt = ui.getElementById('btnDecrypt') as HTMLButtonElement;

const spnCipherSourceLength = ui.getElementById('spnCipherSourceLength') as HTMLSpanElement;
const btnCopyCipherSource = ui.getElementById('btnCopyCipherSource') as HTMLButtonElement;
const btnClearCipherSource = ui.getElementById('btnClearCipherSource') as HTMLButtonElement;

const spnCipherTargetLength = ui.getElementById('spnCipherTargetLength') as HTMLSpanElement;
const btnCopyCipherTarget = ui.getElementById('btnCopyCipherTarget') as HTMLButtonElement;
const btnClearCipherTarget = ui.getElementById('btnClearCipherTarget') as HTMLButtonElement;

let cipherTargetLastChange: string | undefined;

export function findCipherByVersion(version: number): crypto.ICipher | null {
    for (const cipher of ciphers) {
        if (cipher.version === version) {
            return cipher;
        }
    }

    return null;
}

export function findLatestCipher(): crypto.ICipher {
    if (ciphers.length === 0) {
        throw new Error('No ciphers registered.');
    }

    let bestCipher: crypto.ICipher = ciphers[0];

    for (const cipher of ciphers) {
        if (cipher.version > bestCipher.version) {
            bestCipher = cipher;
        }
    }

    return bestCipher;
}

function findCipherVersionDropdownIndexByVersion(version: number): number {
    for (let i = 0; i < ciphers.length; i++) {
        if (ciphers[i].version === version) {
            return i;
        }
    }

    return -1;
}

function findCipherEncodingDropdownIndexByName(name: string): number {
    for (let i = 0; i < availableEncodings.length; i++) {
        if (availableEncodings[i].name === name) {
            return i;
        }
    }

    return -1;
}

function clearSourceVisualCue(): void {
    txtCipherSource.style.removeProperty('background-color');
}

function clearTargetVisualCue(): void {
    txtCipherTarget.style.removeProperty('background-color');
}

function clearEncodingVisualCue(): void {
    cboCipherEncoding.style.removeProperty('background-color');
}

function setSourceVisualCueError() {
    txtCipherSource.style.setProperty('background-color', ui.ERROR_COLOR);
}

function setTargetVisualCueError() {
    txtCipherTarget.style.setProperty('background-color', ui.ERROR_COLOR);
}

function setEncodingVisualCueError() {
    cboCipherEncoding.style.setProperty('background-color', ui.ERROR_COLOR);
}

function clearAllVisualCues(): void {
    clearSourceVisualCue();
    clearTargetVisualCue();
    clearEncodingVisualCue();
}

function clearCipherTargetLastUpdate(): void {
    cipherTargetLastChange = undefined;
}

function updateCipherTargetLastUpdate(): void {
    cipherTargetLastChange = new Date().toISOString();
}

function updateCipherSourceLength(): void {
    spnCipherSourceLength.innerText = txtCipherSource.value.length.toString();
}

function updateCipherTargetLength(): void {
    spnCipherTargetLength.innerText = txtCipherTarget.value.length.toString();
}

function setCipherTargetValue(value: string, isEncrypt: boolean): void {
    const needDateTimeUpdate = value.length > 0 && txtCipherTarget.value !== value;

    txtCipherTarget.value = value;
    updateCipherTargetLength();

    if (needDateTimeUpdate && isEncrypt) {
        updateCipherTargetLastUpdate();
    } else {
        clearCipherTargetLastUpdate();
    }

    onCipherTargetChanged();
}

function onCipherTargetChanged(): void {
    updateCipherParameters();
}

function updateCipherParameters(): void {
    const encodingIndex = cboCipherEncoding.selectedIndex;

    const isValidEncodingIndex = encodingIndex >= 0 && encodingIndex < availableEncodings.length;

    if (txtCipherTarget.value === '' || txtCipherName.value === '' || isValidEncodingIndex === false) {
        storageOutputComponent.clearOutputs();
        return;
    }

    const cipherParameters = {
        datetime: cipherTargetLastChange,
        version: ciphers[cboCipherVersion.selectedIndex].version,
        encoding: availableEncodings[encodingIndex].name,
        value: txtCipherTarget.value
    }

    const path = `ciphers/${txtCipherName.value}`;

    storageOutputComponent.setParameters(cipherParameters, path);
}

export async function encryptString(value: string, encoding: IEncoding, cancellationToken: CancellationToken): Promise<string | null> {
    const privatePart: string = getPrivatePart();

    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    const input: ArrayBuffer = stringUtils.stringToArray(value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const encrypted: ArrayBuffer = await findLatestCipher().encrypt(input, password, cancellationToken);

    ensureNotCancelled(cancellationToken);

    return encoding.encode(encrypted);
}

export async function decryptStringWithCipher(value: string, cipher: crypto.ICipher, encoding: IEncoding, cancellationToken: CancellationToken): Promise<string | null> {
    const privatePart: string = getPrivatePart();

    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    try {
        const input: ArrayBuffer = encoding.decode(value);
        const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

        const decrypted: ArrayBuffer = await cipher.decrypt(input, password, cancellationToken);

        ensureNotCancelled(cancellationToken);

        return arrayUtils.arrayToString(decrypted);
    } catch (error) {
        const typedError = error as Error;

        rethrowCancelled(typedError);

        console.warn(`Failed to decrypt${typedError.message ? `, error: ${typedError.message}` : ', no error message'}`);
        return null;
    }
}

export async function decryptStringWithVersion(value: string, version: number, encoding: IEncoding, cancellationToken: CancellationToken): Promise<string | null> {
    const cipher = findCipherByVersion(version);

    if (cipher === null) {
        throw new Error(`Failed to find cip[her for version ${version}.`);
    }

    return decryptStringWithCipher(value, cipher, encoding, cancellationToken);
}

async function onEncryptButtonClick(): Promise<boolean> {
    txtCipherSource.focus();
    setCipherTargetValue('', true);
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    const encoding: IEncoding = availableEncodings[cboCipherEncoding.selectedIndex];

    const encryptedString: string | null = await encryptString(txtCipherSource.value, encoding, CancellationToken.none);

    if (encryptedString === null) {
        return false;
    }

    setCipherTargetValue(encryptedString, true);
    updateCipherParameters();

    return true;
}

async function onDecryptButtonClick(): Promise<boolean> {
    txtCipherSource.focus();
    setCipherTargetValue('', false);
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    if (cboCipherEncoding.selectedIndex < 0 || cboCipherEncoding.selectedIndex >= availableEncodings.length) {
        setEncodingVisualCueError();
        return false;
    }

    const encoding: IEncoding = availableEncodings[cboCipherEncoding.selectedIndex];

    const decryptedString: string | null = await decryptStringWithCipher(
        txtCipherSource.value,
        ciphers[cboCipherVersion.selectedIndex],
        encoding,
        CancellationToken.none
    );

    if (decryptedString === null) {
        setTargetVisualCueError();
        return false;
    }

    setCipherTargetValue(decryptedString, false);

    return true;
}

function setupCipherVersionsDropdown() {
    for (const cipher of ciphers) {
        const option = document.createElement('option');
        option.text = `v${cipher.version}`;
        cboCipherVersion.appendChild(option);
    }

    cboCipherVersion.selectedIndex = cboCipherVersion.options.length - 1;
}

function setupCipherEncodingDropdown() {
    for (const encoding of availableEncodings) {
        const option = document.createElement('option');
        option.text = encoding.name;
        option.title = encoding.description;
        cboCipherEncoding.appendChild(option);
    }

    cboCipherEncoding.selectedIndex = findCipherEncodingDropdownIndexByName(RECOMMENDED_ENCODING_NAME);
}

export class CipherComponent implements IComponent, ITabInfo {
    public readonly name: string = 'Cipher';

    public getTabButton(): HTMLButtonElement {
        return btnTabCiphers;
    }

    public getTabContent(): HTMLElement {
        return divTabCiphers;
    }

    public onTabSelected(): void {
        storageOutputComponent.show();
        updateCipherParameters();
        txtCipherName.focus();
    }

    private static fullPathToStoragePath(fullPath: string, cipherName: string): string | null {
        const prefix = '<root>/';
        const suffix = `/ciphers/${cipherName}`;

        if (fullPath.startsWith(prefix) === false) {
            return null;
        }

        if (fullPath.endsWith(suffix) === false) {
            return null;
        }

        return fullPath.substring(prefix.length, fullPath.length - suffix.length);
    }

    public async setParameters(cipherName: string, parameterKeys: PlainObject, storageFullPath: string): Promise<boolean> {
        txtCipherName.value = '';
        txtCipherSource.value = '';
        updateCipherSourceLength();
        txtCipherTarget.value = '';
        updateCipherTargetLength();
        cboCipherVersion.selectedIndex = cboCipherVersion.options.length - 1;
        cboCipherEncoding.selectedIndex = findCipherEncodingDropdownIndexByName(RECOMMENDED_ENCODING_NAME);
        storageOutputComponent.setPathUI('');
        storageOutputComponent.setCustomKeysUI('');

        const encodingName: string = parameterKeys.encoding ?? LEGACY_ENCODING_NAME;

        const encoding: IEncoding | null = findEncodingByName(encodingName);

        if (encoding === null) {
            throw new Error(`Failed to find encoding '${encodingName}'.`);
        }

        const decrypted: string | null = await decryptStringWithVersion(
            parameterKeys.value,
            parameterKeys.version,
            encoding,
            CancellationToken.none
        );

        if (decrypted === null) {
            alert(`Failed to decrypt cipher '${cipherName}'.`);
            return false;
        }

        const storagePath: string | null = CipherComponent.fullPathToStoragePath(storageFullPath, cipherName);

        if (storagePath === null) {
            console.error(`Failed to retrieve storage path from full path '${storageFullPath}'.`);
            alert('Failed to retrieve storage path from full path.');
            return false;
        }

        if (parameterKeys.customKeys) {
            storageOutputComponent.setCustomKeysUI(JSON.stringify(parameterKeys.customKeys, null, 4));
        }

        delete parameterKeys.customKeys;

        txtCipherName.value = cipherName;

        txtCipherSource.value = decrypted;
        updateCipherSourceLength();

        cboCipherVersion.selectedIndex = findCipherVersionDropdownIndexByVersion(parameterKeys.version);
        cboCipherEncoding.selectedIndex = findCipherEncodingDropdownIndexByName(encodingName);

        storageOutputComponent.setPathUI(storagePath);
        storageOutputComponent.setParameters(parameterKeys, `ciphers/${cipherName}`);

        return true;
    }

    public getVaultHint(): string {
        return `${this.name.toLowerCase()} '${txtCipherName.value}'`;
    }

    public init(): void {
        const errorLogsService = serviceManager.getService('errorLogs');
        const logFunc = errorLogsService.createLogErrorMessageFunction();

        ui.setupCopyButton(txtCipherSource, btnCopyCipherSource, logFunc);
        ui.setupCopyButton(txtCipherTarget, btnCopyCipherTarget, logFunc);

        ui.setupFeedbackButton(btnEncrypt, onEncryptButtonClick, logFunc);
        ui.setupFeedbackButton(btnDecrypt, onDecryptButtonClick, logFunc);

        txtCipherName.addEventListener('input', () => {
            updateCipherParameters();
        });

        txtCipherSource.addEventListener('input', () => {
            updateCipherSourceLength();
            if (txtCipherSource.value.length > 0) {
                clearSourceVisualCue();
            }
        });

        txtCipherTarget.addEventListener('input', () => {
            updateCipherTargetLength();
        });

        cboCipherVersion.addEventListener('input', () => {
            updateCipherParameters();
        });

        btnClearAllCipherInfo.addEventListener('click', () => {
            txtCipherName.value = '';
            txtCipherSource.value = '';
            updateCipherSourceLength();
            txtCipherTarget.value = '';
            updateCipherTargetLength();
            cboCipherVersion.selectedIndex = cboCipherVersion.options.length - 1;
            cboCipherEncoding.selectedIndex = findCipherEncodingDropdownIndexByName(RECOMMENDED_ENCODING_NAME);
            storageOutputComponent.clearMatchingPath();
            clearCipherTargetLastUpdate();
            clearAllVisualCues();

            storageOutputComponent.clearUI();
        });

        btnClearCipherSource.addEventListener('click', () => {
            ui.clearText(txtCipherSource, true);
        });

        btnClearCipherTarget.addEventListener('click', () => {
            setCipherTargetValue('', false);
        });

        setupCipherVersionsDropdown();
        setupCipherEncodingDropdown();

        serviceManager.registerService('cipher', new CipherService(this));
    }
}
