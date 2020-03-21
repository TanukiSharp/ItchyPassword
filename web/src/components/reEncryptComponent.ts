import { ICipher } from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';
import { ITabInfo } from '../TabControl';
import * as ui from '../ui';
import { getPrivatePart } from './privatePartComponent';

import { CipherV1 } from '../ciphers/v1';
import { CipherV2 } from '../ciphers/v2';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

import { CancellationToken } from '../asyncUtils';

const ciphers: ICipher[] = [
    new CipherV1(),
    new CipherV2()
];

const btnTabReEncrypt: HTMLInputElement = ui.getElementById('btnTabReEncrypt');
const divTabReEncrypt: HTMLInputElement = ui.getElementById('divTabReEncrypt');

const txtReEncryptSource: HTMLInputElement = ui.getElementById('txtReEncryptSource');
const txtReEncryptTarget: HTMLInputElement = ui.getElementById('txtReEncryptTarget');

const cboReEncryptFrom: HTMLInputElement = ui.getElementById('cboReEncryptFrom');
const cboReEncryptTo: HTMLInputElement = ui.getElementById('cboReEncryptTo');
const btnReEncrypt: HTMLInputElement = ui.getElementById('btnReEncrypt');

const btnClearReEncryptSource: HTMLInputElement = ui.getElementById('btnClearReEncryptSource');
const btnCopyReEncryptTarget: HTMLInputElement = ui.getElementById('btnCopyReEncryptTarget');
const btnClearReEncryptTarget: HTMLInputElement = ui.getElementById('btnClearReEncryptTarget');

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
    txtReEncryptSource.style.setProperty('background-color', ui.ERROR_COLOR);
}

function setTargetVisualCueError() {
    txtReEncryptTarget.style.setProperty('background-color', ui.ERROR_COLOR);
}

function clearAllVisualCues(): void {
    clearSourceVisualCue();
    clearTargetVisualCue();
}

async function onReEncryptButtonClick(): Promise<boolean> {
    txtReEncryptTarget.value = '';
    clearAllVisualCues();

    if (txtReEncryptSource.value.length === 0) {
        setSourceVisualCueError();
        return false;
    }

    if (cboReEncryptFrom.value === cboReEncryptTo.value) {
        setTargetVisualCueError();
        return false;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return false;
    }

    const sourceCipherIndex = parseInt(cboReEncryptFrom.value, 10);
    const targetCipherIndex = parseInt(cboReEncryptTo.value, 10);

    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const input: ArrayBuffer = stringUtils.fromBase16(txtReEncryptSource.value);
    const decrypted: ArrayBuffer = await ciphers[sourceCipherIndex].decrypt(input, password, CancellationToken.none);
    const reEncrypted: ArrayBuffer = await ciphers[targetCipherIndex].encrypt(decrypted, password, CancellationToken.none);

    txtReEncryptTarget.value = arrayUtils.toBase16(reEncrypted);

    return true;
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
        ui.setupCopyButton(txtReEncryptTarget, btnCopyReEncryptTarget);

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

        ui.setupFeedbackButton(btnReEncrypt, onReEncryptButtonClick);
    }
}
