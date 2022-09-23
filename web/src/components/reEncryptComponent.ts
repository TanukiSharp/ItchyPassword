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

const btnTabReEncrypt: HTMLButtonElement = ui.getElementById('btnTabReEncrypt') as HTMLButtonElement;
const divTabReEncrypt: HTMLElement = ui.getElementById('divTabReEncrypt');

const txtReEncryptSource: HTMLInputElement = ui.getElementById('txtReEncryptSource') as HTMLInputElement;
const txtReEncryptTarget: HTMLInputElement = ui.getElementById('txtReEncryptTarget') as HTMLInputElement;

const cboReEncryptFrom: HTMLSelectElement = ui.getElementById('cboReEncryptFrom') as HTMLSelectElement;
const cboReEncryptTo: HTMLSelectElement = ui.getElementById('cboReEncryptTo') as HTMLSelectElement;
const btnReEncrypt: HTMLButtonElement = ui.getElementById('btnReEncrypt') as HTMLButtonElement;

const btnClearReEncryptSource: HTMLButtonElement = ui.getElementById('btnClearReEncryptSource') as HTMLButtonElement;
const btnCopyReEncryptTarget: HTMLButtonElement = ui.getElementById('btnCopyReEncryptTarget') as HTMLButtonElement;
const btnClearReEncryptTarget: HTMLButtonElement = ui.getElementById('btnClearReEncryptTarget') as HTMLButtonElement;

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
    ui.clearText(txtReEncryptTarget, true);
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
    public readonly name: string = 'ReEncrypt';

    public getTabButton(): HTMLButtonElement {
        return btnTabReEncrypt;
    }

    public getTabContent(): HTMLElement {
        return divTabReEncrypt;
    }

    public onTabSelected() {
        storageOutputComponent.hide();
        txtReEncryptSource.focus();
    }

    public getVaultHint(): string {
        throw new Error('Not supported.');
    }

    public init(): void {
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
            ui.clearText(txtReEncryptSource, true);
        });

        btnClearReEncryptTarget.addEventListener('click', () => {
            ui.clearText(txtReEncryptTarget, true);
        });

        ui.setupFeedbackButton(btnReEncrypt, onReEncryptButtonClick);
    }
}
