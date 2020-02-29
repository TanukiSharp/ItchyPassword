import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import * as ui from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { CipherV2 } from '../ciphers/v2';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

const RESERVED_KEYS: string[] = ['version', 'value'];

const btnTabCiphers: HTMLInputElement = ui.getElementById('btnTabCiphers');
const divTabCiphers: HTMLInputElement = ui.getElementById('divTabCiphers');

const cipher: crypto.ICipher = new CipherV2();

const txtCipherName: HTMLInputElement = ui.getElementById('txtCipherName');
const txtCipherSource: HTMLInputElement = ui.getElementById('txtCipherSource');
const txtCipherTarget: HTMLInputElement = ui.getElementById('txtCipherTarget');
const btnEncrypt: HTMLInputElement = ui.getElementById('btnEncrypt');
const btnDecrypt: HTMLInputElement = ui.getElementById('btnDecrypt');

const btnClearCipherSource: HTMLInputElement = ui.getElementById('btnClearCipherSource');
const btnCopyCipherTarget: HTMLInputElement = ui.getElementById('btnCopyCipherTarget');
const btnClearCipherTarget: HTMLInputElement = ui.getElementById('btnClearCipherTarget');

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

function setCipherTargetValue(value: string): void {
    txtCipherTarget.value = value;
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
        version: cipher.version,
        value: txtCipherTarget.value
    }

    const path = `ciphers/${txtCipherName.value}`;

    storageOutputComponent.setParameters(cipherParameters, path, RESERVED_KEYS);
}

export async function encryptString(value: string): Promise<string | null> {
    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    const input: ArrayBuffer = stringUtils.stringToArray(value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const encrypted: ArrayBuffer = await cipher.encrypt(input, password);

    return arrayUtils.toCustomBase(encrypted, crypto.BASE62_ALPHABET);
}

export async function decryptString(value: string): Promise<string | null> {
    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return null;
    }

    try {
        const input: ArrayBuffer = arrayUtils.fromCustomBase(value, crypto.BASE62_ALPHABET);
        const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

        const decrypted: ArrayBuffer = await cipher.decrypt(input, password);

        return arrayUtils.arrayToString(decrypted);
    } catch (error) {
        console.warn(`Failed to decrypt${error.message ? `, error: ${error.message}` : ', no error message'}`);
        return null;
    }
}

async function onEncryptButtonClick(): Promise<boolean> {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    const encryptedString: string | null = await encryptString(txtCipherSource.value);

    if (encryptedString === null) {
        return false;
    }

    setCipherTargetValue(encryptedString);

    return true;
}

async function onDecryptButtonClick(): Promise<boolean> {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    const decryptedString: string | null = await decryptString(txtCipherSource.value);

    if (decryptedString === null) {
        setTargetVisualCueError();
        return false;
    }

    setCipherTargetValue(decryptedString);

    return true;
}

export class CipherComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabCiphers;
    }
    getTabContent(): HTMLInputElement {
        return divTabCiphers;
    }
    onTabSelected(): void {
        storageOutputComponent.show();
        updateCipherParameters();
    }

    init(): void {
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

        btnClearCipherSource.addEventListener('click', () => {
            txtCipherSource.value = '';
        });

        btnClearCipherTarget.addEventListener('click', () => {
            setCipherTargetValue('');
        });
    }
}
