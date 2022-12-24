import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import * as ui from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { CipherV2 } from '../ciphers/v2';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

import * as serviceManager from '../services/serviceManger';
import { CipherService } from '../services/cipherService';

import { CancellationToken, ensureNotCancelled, rethrowCancelled } from '../asyncUtils';
import { PlainObject } from '../PlainObject';

const btnTabCiphers = ui.getElementById('btnTabCiphers') as HTMLButtonElement;
const divTabCiphers = ui.getElementById('divTabCiphers');

const cipher: crypto.ICipher = new CipherV2();

const btnClearAllCipherInfo = ui.getElementById('btnClearAllCipherInfo') as HTMLButtonElement;

const txtCipherName = ui.getElementById('txtCipherName') as HTMLInputElement;
const txtCipherSource = ui.getElementById('txtCipherSource') as HTMLInputElement;
const txtCipherTarget = ui.getElementById('txtCipherTarget') as HTMLInputElement;

const btnEncrypt = ui.getElementById('btnEncrypt') as HTMLButtonElement;
const btnDecrypt = ui.getElementById('btnDecrypt') as HTMLButtonElement;

const btnCopyCipherSource = ui.getElementById('btnCopyCipherSource') as HTMLButtonElement;
const btnClearCipherSource = ui.getElementById('btnClearCipherSource') as HTMLButtonElement;
const btnCopyCipherTarget = ui.getElementById('btnCopyCipherTarget') as HTMLButtonElement;
const btnClearCipherTarget = ui.getElementById('btnClearCipherTarget') as HTMLButtonElement;

let cipherTargetLastChange: string | undefined;

function clearSourceVisualCue(): void {
    txtCipherSource.style.removeProperty('background-color');
}

function clearTargetVisualCue(): void {
    txtCipherTarget.style.removeProperty('background-color');
}

function setSourceVisualCueError() {
    txtCipherSource.style.setProperty('background-color', ui.ERROR_COLOR);
}

function setTargetVisualCueError() {
    txtCipherTarget.style.setProperty('background-color', ui.ERROR_COLOR);
}

function clearAllVisualCues(): void {
    clearSourceVisualCue();
    clearTargetVisualCue();
}

function clearCipherTargetLastUpdate(): void {
    cipherTargetLastChange = undefined;
}

function updateCipherTargetLastUpdate(): void {
    cipherTargetLastChange = new Date().toISOString();
}

function setCipherTargetValue(value: string, isEncrypt: boolean): void {
    const needDateTimeUpdate = value.length > 0 && txtCipherTarget.value !== value;

    txtCipherTarget.value = value;

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
    if (txtCipherTarget.value === '' || txtCipherName.value === '') {
        storageOutputComponent.clearOutputs();
        return;
    }

    const cipherParameters = {
        datetime: cipherTargetLastChange,
        version: cipher.version,
        value: txtCipherTarget.value
    }

    const path = `ciphers/${txtCipherName.value}`;

    storageOutputComponent.setParameters(cipherParameters, path);
}

export async function encryptString(value: string, cancellationToken: CancellationToken): Promise<string | null> {
    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    const input: ArrayBuffer = stringUtils.stringToArray(value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const encrypted: ArrayBuffer = await cipher.encrypt(input, password, cancellationToken);

    ensureNotCancelled(cancellationToken);

    return arrayUtils.toCustomBase(encrypted, crypto.BASE62_ALPHABET);
}

export async function decryptString(value: string, cancellationToken: CancellationToken): Promise<string | null> {
    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    try {
        const input: ArrayBuffer = arrayUtils.fromCustomBase(value, crypto.BASE62_ALPHABET);
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

async function onEncryptButtonClick(): Promise<boolean> {
    txtCipherSource.focus();
    setCipherTargetValue('', true);
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    const encryptedString: string | null = await encryptString(txtCipherSource.value, CancellationToken.none);

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

    const decryptedString: string | null = await decryptString(txtCipherSource.value, CancellationToken.none);

    if (decryptedString === null) {
        setTargetVisualCueError();
        return false;
    }

    setCipherTargetValue(decryptedString, false);

    return true;
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
        txtCipherTarget.value = '';
        storageOutputComponent.setPathUI('');
        storageOutputComponent.setCustomKeysUI('');

        const decrypted: string | null = await decryptString(parameterKeys.value, CancellationToken.none);

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

        storageOutputComponent.setPathUI(storagePath);
        storageOutputComponent.setParameters(parameterKeys, `ciphers/${cipherName}`);

        return true;
    }

    public getVaultHint(): string {
        return `${this.name.toLowerCase()} '${txtCipherName.value}'`;
    }

    public init(): void {
        ui.setupCopyButton(txtCipherSource, btnCopyCipherSource);
        ui.setupCopyButton(txtCipherTarget, btnCopyCipherTarget);

        ui.setupFeedbackButton(btnEncrypt, onEncryptButtonClick);
        ui.setupFeedbackButton(btnDecrypt, onDecryptButtonClick);

        txtCipherName.addEventListener('input', () => {
            updateCipherParameters();
        });

        txtCipherSource.addEventListener('input', () => {
            if (txtCipherSource.value.length > 0) {
                clearSourceVisualCue();
            }
        });

        btnClearAllCipherInfo.addEventListener('click', () => {
            txtCipherName.value = '';
            txtCipherSource.value = '';
            txtCipherTarget.value = '';
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

        serviceManager.registerService('cipher', new CipherService(this));
    }
}
