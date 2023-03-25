import * as ui from '../ui';
import * as privatePartComponent from './privatePartComponent';

import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import { PlainObject } from '../PlainObject';
import { PasswordGeneratorV1 } from '../passwordGenerators/v1';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

import { CancellationToken, TaskRunner } from '../asyncUtils';

import * as serviceManager from '../services/serviceManger';
import { PasswordService } from '../services/passwordService';

const btnTabPasswords = ui.getElementById('btnTabPasswords') as HTMLButtonElement;
const divTabPasswords = ui.getElementById('divTabPasswords');

const passwordGenerator: crypto.IPasswordGenerator = new PasswordGeneratorV1('Password');

const txtPublicPart = ui.getElementById('txtPublicPart') as HTMLInputElement;
const spnPublicPartSize = ui.getElementById('spnPublicPartSize');
const btnGeneratePublicPart = ui.getElementById('btnGeneratePublicPart') as HTMLButtonElement;
const btnClearPublicPart = ui.getElementById('btnClearPublicPart') as HTMLButtonElement;
const btnCopyPublicPart = ui.getElementById('btnCopyPublicPart') as HTMLButtonElement;
const btnShowHidePasswordOptionalFeatures = ui.getElementById('btnShowHidePasswordOptionalFeatures') as HTMLButtonElement;

const lblAlphabetLength = ui.getElementById('lblAlphabetLength');
const numOutputSizeRange = ui.getElementById('numOutputSizeRange') as HTMLInputElement;
const numOutputSizeNum = ui.getElementById('numOutputSizeNum') as HTMLInputElement;

const lblAlphabet = ui.getElementById('lblAlphabet');
const txtAlphabet = ui.getElementById('txtAlphabet') as HTMLInputElement;
const spnAlphabetSize = ui.getElementById('spnAlphabetSize');
const divPasswordAlphabetActions = ui.getElementById('divPasswordAlphabetActions');
const btnResetAlphabet = ui.getElementById('btnResetAlphabet') as HTMLButtonElement;

const txtResultPassword = ui.getElementById('txtResultPassword') as HTMLInputElement;
const spnResultPasswordLength = ui.getElementById('spnResultPasswordLength');
const btnViewResultPassword = ui.getElementById('btnViewResultPassword') as HTMLButtonElement;
const btnCopyResultPassword = ui.getElementById('btnCopyResultPassword') as HTMLButtonElement;
const lblGeneratingPassword = ui.getElementById('lblGeneratingPassword');

export const DEFAULT_LENGTH: number = 64;
export const DEFAULT_ALPHABET: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

let passwordPublicPartLastChange: string | undefined;
let copyPasswordFunction: () => void;

function onClearPublicPartButtonClick(): boolean {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to clear the public part ?\nType \'y\' to accept', '') !== 'y') {
            return false;
        }
    }

    ui.clearText(txtPublicPart, true);

    updatePublicPartSize();

    updatePasswordPublicPartLastUpdate();
    updatePasswordGenerationParameters();

    return true;
}

function onGeneratePublicPartButtonClick(): boolean {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to generate a new public part ?\nType \'y\' to accept', '') !== 'y') {
            return false;
        }
    }

    const randomString: string = crypto.generateRandomString();
    txtPublicPart.value = randomString;
    updatePublicPartSize();

    updatePasswordPublicPartLastUpdate();

    run();

    return true;
}

function updatePasswordPublicPartLastUpdate(): void {
    if (txtPublicPart.value.length > 0) {
        passwordPublicPartLastChange = new Date().toISOString();
    } else {
        passwordPublicPartLastChange = undefined;
    }
}

function updateResultPasswordLength(): void {
    spnResultPasswordLength.innerHTML = txtResultPassword.value.length.toString();
}

function isAlphabetValid(alphabet: string): boolean {
    const sortedAlphabet: string[] = alphabet.split('');
    sortedAlphabet.sort();

    for (let i: number = 1; i < sortedAlphabet.length; i += 1) {
        if (sortedAlphabet[i - 1] === sortedAlphabet[i]) {
            return false;
        }
    }

    return true;
}

function updatePasswordGenerationParameters(): void {
    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const passwordParamters: PlainObject = {
        public: txtPublicPart.value,
        datetime: passwordPublicPartLastChange,
        version: passwordGenerator.version,
        length: txtResultPassword.value.length,
        alphabet: txtAlphabet.value
    };

    storageOutputComponent.setParameters(passwordParamters, 'password');
}

function updateOutputSizeRangeToNum(): void {
    numOutputSizeNum.value = numOutputSizeRange.value;
}

function updateOutputSizeNumToRange(): boolean {
    const min: number = parseInt(numOutputSizeRange.min, 10);
    const val: number = parseInt(numOutputSizeNum.value, 10);
    const max: number = parseInt(numOutputSizeRange.max, 10);

    if (isNaN(val) === false) {
        numOutputSizeRange.value = Math.max(min, Math.min(val, max)).toString();
        return true;
    }

    return false;
}

async function onOutputSizeRangeInput(): Promise<void> {
    updateOutputSizeRangeToNum();
    await run();
}

async function onOutputSizeNumInput(): Promise<void> {
    if (updateOutputSizeNumToRange()) {
        updateOutputSizeRangeToNum();
    }
    await run();
}

function updatePublicPartSize(): void {
    spnPublicPartSize.innerHTML = txtPublicPart.value.length.toString();
}

function updateAlphabetSize(): void {
    spnAlphabetSize.innerHTML = txtAlphabet.value.length.toString();
}

function updateAlphabetValidityDisplay(isAlphabetValid: boolean): void {
    if (isAlphabetValid) {
        txtAlphabet.style.removeProperty('background');
    } else {
        txtAlphabet.style.setProperty('background', ui.ERROR_COLOR);
    }
}

async function onAlphabetTextInput(): Promise<void> {
    const isAlphabetValidResult: boolean = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult === false) {
        return;
    }

    updateAlphabetSize();
    await run();
}

async function onResetAlphabetButtonClick(): Promise<boolean> {
    if (resetAlphabet() === false) {
        return false;
    }

    await run();

    return true;
}

function clearOutputs(): void {
    ui.clearText(txtResultPassword);
    storageOutputComponent.clearOutputs();
    updateResultPasswordLength();
}

function canRun(publicPart?: string): boolean {
    const alphabet: string = txtAlphabet.value;

    if (isAlphabetValid(alphabet) === false) {
        return false;
    }

    publicPart = publicPart || txtPublicPart.value;

    if (privatePartComponent.getPrivatePart().length <= 0 || publicPart.length < 8 || alphabet.length < 2) {
        return false;
    }

    return true;
}

export async function generatePasswordString(publicPart: string, alphabet: string, cancellationToken: CancellationToken): Promise<string | null> {
    if (canRun(publicPart) === false) {
        return null;
    }

    const privatePartString: string = privatePartComponent.getPrivatePart();
    const privatePrivateBytes: ArrayBuffer = stringUtils.stringToArray(privatePartString);
    const publicPartBytes: ArrayBuffer = stringUtils.stringToArray(publicPart);
    const keyBytes: ArrayBuffer = await passwordGenerator.generatePassword(privatePrivateBytes, publicPartBytes, cancellationToken);

    return arrayUtils.toCustomBaseOneWay(keyBytes, alphabet);
}

const passwordTaskRunner: TaskRunner<void> = new TaskRunner<void>();

export async function run(): Promise<void> {
    if (canRun() === false) {
        clearOutputs();
        return;
    }

    ui.showHide(lblGeneratingPassword, true);

    try {
        await passwordTaskRunner.cancelAndExecute(runCore);
    } finally {
        ui.showHide(lblGeneratingPassword, false);
    }
}

async function runCore(cancellationToken: CancellationToken): Promise<void> {
    const keyString: string | null = await generatePasswordString(txtPublicPart.value, txtAlphabet.value, cancellationToken);
    if (keyString === null) {
        return;
    }

    txtResultPassword.value = stringUtils.truncate(keyString, Math.max(4, parseInt(numOutputSizeRange.value, 10)));

    updateResultPasswordLength();
    updatePasswordGenerationParameters();

    copyPasswordFunction();
}

function resetAlphabet(): boolean {
    txtAlphabet.value = DEFAULT_ALPHABET;
    updateAlphabetSize();

    const isAlphabetValidResult: boolean = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    return isAlphabetValidResult;
}

async function onPublicPartTextInput(): Promise<void> {
    updatePublicPartSize();
    updatePasswordPublicPartLastUpdate();
    await run();
}

export class PasswordComponent implements IComponent, ITabInfo {
    public readonly name: string = 'Password';

    public getTabButton(): HTMLButtonElement {
        return btnTabPasswords;
    }

    public getTabContent(): HTMLElement {
        return divTabPasswords;
    }

    public onTabSelected(): void {
        storageOutputComponent.show();
        updatePasswordGenerationParameters();
        txtPublicPart.focus();
    }

    public getVaultHint(): string {
        return this.name.toLowerCase();
    }

    public init(): void {
        const errorLogsService = serviceManager.getService('errorLogs');
        const logFunc = errorLogsService.createLogErrorMessageFunction();

        privatePartComponent.registerOnChanged(run);

        // dafuq!?
        numOutputSizeRange.max = DEFAULT_LENGTH.toString();
        numOutputSizeRange.value = DEFAULT_LENGTH.toString();

        ui.setupFeedbackButton(btnClearPublicPart, onClearPublicPartButtonClick, logFunc);
        ui.setupFeedbackButton(btnGeneratePublicPart, onGeneratePublicPartButtonClick, logFunc);

        ui.setupViewButton(txtResultPassword, btnViewResultPassword);

        ui.setupCopyButton(txtPublicPart, btnCopyPublicPart, logFunc);
        copyPasswordFunction = ui.setupCopyButton(txtResultPassword, btnCopyResultPassword, logFunc);

        numOutputSizeRange.addEventListener('input', onOutputSizeRangeInput);
        numOutputSizeNum.addEventListener('input', onOutputSizeNumInput);

        txtAlphabet.addEventListener('input', onAlphabetTextInput);
        ui.setupFeedbackButton(btnResetAlphabet, onResetAlphabetButtonClick, logFunc);

        txtPublicPart.addEventListener('input', onPublicPartTextInput);

        ui.showHide(lblGeneratingPassword, false);

        ui.setupShowHideButton(btnShowHidePasswordOptionalFeatures, false, [
            lblAlphabet,
            txtAlphabet,
            spnAlphabetSize,
            divPasswordAlphabetActions,
            lblAlphabetLength,
            numOutputSizeRange,
            numOutputSizeNum
        ]);

        updatePublicPartSize();
        updateOutputSizeRangeToNum();
        resetAlphabet();

        serviceManager.registerService('password', new PasswordService());
    }
};
