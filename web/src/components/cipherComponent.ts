import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import { getElementById, setupCopyButton, ERROR_COLOR } from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { CipherV2 } from '../ciphers/v2';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

const RESERVED_KEYS: string[] = ['version', 'value'];

const btnTabCiphers: HTMLInputElement = getElementById('btnTabCiphers');
const divTabCiphers: HTMLInputElement = getElementById('divTabCiphers');

const cipher: crypto.ICipher = new CipherV2();

const txtCipherName: HTMLInputElement = getElementById('txtCipherName');
const txtCipherSource: HTMLInputElement = getElementById('txtCipherSource');
const txtCipherTarget: HTMLInputElement = getElementById('txtCipherTarget');
const btnEncrypt: HTMLInputElement = getElementById('btnEncrypt');
const btnDecrypt: HTMLInputElement = getElementById('btnDecrypt');

const btnClearCipherSource: HTMLInputElement = getElementById('btnClearCipherSource');
const btnCopyCipherTarget: HTMLInputElement = getElementById('btnCopyCipherTarget');
const btnClearCipherTarget: HTMLInputElement = getElementById('btnClearCipherTarget');

function clearSourceVisualCue(): void {
    txtCipherSource.style.removeProperty('background-color');
}

function clearTargetVisualCue(): void {
    txtCipherTarget.style.removeProperty('background-color');
}

function setSourceVisualCueError() {
    txtCipherSource.style.setProperty('background-color', ERROR_COLOR);
}

function setTargetVisualCueError() {
    txtCipherTarget.style.setProperty('background-color', ERROR_COLOR);
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

async function onEncryptButtonClick(): Promise<void> {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }

    const input: ArrayBuffer = stringUtils.stringToArray(txtCipherSource.value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const encrypted: ArrayBuffer = await cipher.encrypt(input, password);

    setCipherTargetValue(arrayUtils.toCustomBase(encrypted, crypto.BASE62_ALPHABET));
}

async function onDecryptButtonClick(): Promise<void> {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();

    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }

    try {
        const input: ArrayBuffer = arrayUtils.fromCustomBase(txtCipherSource.value, crypto.BASE62_ALPHABET);
        const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

        const decrypted: ArrayBuffer = await cipher.decrypt(input, password);

        setCipherTargetValue(arrayUtils.arrayToString(decrypted));
    } catch (error) {
        console.warn(`Failed to decrypt${error.message ? `, error: ${error.message}` : ', no error message'}`);
        setTargetVisualCueError();
    }
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
        setupCopyButton(txtCipherTarget, btnCopyCipherTarget);

        btnEncrypt.addEventListener('click', onEncryptButtonClick);
        btnDecrypt.addEventListener('click', onDecryptButtonClick);

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
