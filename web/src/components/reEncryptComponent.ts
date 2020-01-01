import { ICipher } from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';
import { ITabInfo } from '../TabControl';
import { getElementById, setupCopyButton, ERROR_COLOR } from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { CipherV1 } from '../ciphers/v1';
import { CipherV2 } from '../ciphers/v2';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

const ciphers: ICipher[] = [
    new CipherV1(),
    new CipherV2()
];

const btnTabReEncrypt: HTMLInputElement = getElementById('btnTabReEncrypt');
const divTabReEncrypt: HTMLInputElement = getElementById('divTabReEncrypt');

const txtReEncryptSource: HTMLInputElement = getElementById('txtReEncryptSource');
const txtReEncryptTarget: HTMLInputElement = getElementById('txtReEncryptTarget');

const cboReEncryptFrom: HTMLInputElement = getElementById('cboReEncryptFrom');
const cboReEncryptTo: HTMLInputElement = getElementById('cboReEncryptTo');
const btnReEncrypt: HTMLInputElement = getElementById('btnReEncrypt');

const btnClearReEncryptSource: HTMLInputElement = getElementById('btnClearReEncryptSource');
const btnCopyReEncryptTarget: HTMLInputElement = getElementById('btnCopyReEncryptTarget');
const btnClearReEncryptTarget: HTMLInputElement = getElementById('btnClearReEncryptTarget');

function fillCipherComboBox(cbo: HTMLSelectElement, initialValue: number): void {
    let cipher: ICipher;

    for (cipher of ciphers) {
        const item: HTMLOptionElement = document.createElement('option');
        item.value = cbo.childNodes.length.toString();
        item.text = `${cipher.description} (v${cipher.version})`;
        cbo.add(item);
    }

    cbo.value = initialValue.toString();
}

function clearSourceVisualCue(): void {
    txtReEncryptSource.style.removeProperty('background-color');
}

function clearTargetVisualCue(): void {
    txtReEncryptTarget.style.removeProperty('background-color');
}

function setSourceVisualCueError() {
    txtReEncryptSource.style.setProperty('background-color', ERROR_COLOR);
}

function setTargetVisualCueError() {
    txtReEncryptTarget.style.setProperty('background-color', ERROR_COLOR);
}

function clearAllVisualCues(): void {
    clearSourceVisualCue();
    clearTargetVisualCue();
}

async function onReEncryptButtonClick(): Promise<void> {
    txtReEncryptTarget.value = '';
    clearAllVisualCues();

    if (txtReEncryptSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }

    if (cboReEncryptFrom.value === cboReEncryptTo.value) {
        setTargetVisualCueError();
        return;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }

    const sourceCipherIndex = parseInt(cboReEncryptFrom.value, 10);
    const targetCipherIndex = parseInt(cboReEncryptTo.value, 10);

    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const input: ArrayBuffer = stringUtils.fromBase16(txtReEncryptSource.value);
    const decrypted: ArrayBuffer = await ciphers[sourceCipherIndex].decrypt(input, password);
    const reEncrypted: ArrayBuffer = await ciphers[targetCipherIndex].encrypt(decrypted, password);

    txtReEncryptTarget.value = arrayUtils.toBase16(reEncrypted);
}

export class ReEncryptComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabReEncrypt;
    }
    getTabContent(): HTMLInputElement {
        return divTabReEncrypt;
    }
    onTabSelected() {
        storageOutputComponent.hide();
    }

    init(): void {
        setupCopyButton(txtReEncryptTarget, btnCopyReEncryptTarget);

        // Mais est-ce que ce monde est serieux?
        fillCipherComboBox(<HTMLSelectElement><any>cboReEncryptFrom, ciphers.length - 2);
        fillCipherComboBox(<HTMLSelectElement><any>cboReEncryptTo, ciphers.length - 1);

        txtReEncryptSource.addEventListener('input', () => {
            if (txtReEncryptSource.value.length > 0) {
                clearSourceVisualCue();
            }
        });

        btnClearReEncryptSource.addEventListener('click', () => {
            txtReEncryptSource.value = '';
        });

        btnClearReEncryptTarget.addEventListener('click', () => {
            txtReEncryptTarget.value = '';
        });

        btnReEncrypt.addEventListener('click', onReEncryptButtonClick);
    }
}
