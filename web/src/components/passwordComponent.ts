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

const DEFAULT_LENGTH: number = 64;
const DEFAULT_ALPHABET: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const RESERVED_KEYS: string[] = ['alphabet', 'length', 'public', 'datetime'];

let passwordPublicPartLastChange: string | undefined;
let copyPasswordFunction: () => void;

function onClearPublicPartButtonClick(): boolean {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to clear the public part ?\nType \'y\' to accept', '') !== 'y') {
            return false;
        }
    }

    txtPublicPart.value = '';
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

    run(CancellationToken.none);

    return true;
}

function updatePasswordPublicPartLastUpdate(): void {
    if (txtPublicPart.value.length > 0) {
        passwordPublicPartLastChange = new Date().toISOString();
    } else {
        passwordPublicPartLastChange = undefined;
    }
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
        datetime: passwordPublicPartLastChange
    };

    const numericValue: number = txtResultPassword.value.length;
    if (numericValue !== DEFAULT_LENGTH) {
        passwordParamters.length = numericValue;
    }

    const alphabet: string = txtAlphabet.value;
    if (alphabet !== DEFAULT_ALPHABET) {
        passwordParamters.alphabet = alphabet;
    }

    storageOutputComponent.setParameters(passwordParamters, 'password', RESERVED_KEYS);
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
    await run(CancellationToken.none);
}

async function onOutputSizeNumInput(): Promise<void> {
    if (updateOutputSizeNumToRange()) {
        updateOutputSizeRangeToNum();
    }
    await run(CancellationToken.none);
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
    await run(CancellationToken.none);
}

async function onResetAlphabetButtonClick(): Promise<boolean> {
    if (resetAlphabet() === false) {
        return false;
    }

    await run(CancellationToken.none);

    return true;
}

function clearOutputs(): void {
    txtResultPassword.value = '';
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

export async function generatePasswordString(publicPart: string, cancellationToken: CancellationToken): Promise<string | null> {
    if (canRun(publicPart) === false) {
        return null;
    }

    const privatePartString: string = privatePartComponent.getPrivatePart();
    const privatePrivateBytes: ArrayBuffer = stringUtils.stringToArray(privatePartString);
    const publicPartBytes: ArrayBuffer = stringUtils.stringToArray(publicPart);
    const keyBytes: ArrayBuffer = await passwordGenerator.generatePassword(privatePrivateBytes, publicPartBytes, cancellationToken);

    return arrayUtils.toCustomBaseOneWay(keyBytes, txtAlphabet.value);
}

const passwordTaskRunner: TaskRunner<void> = new TaskRunner<void>();

async function run(cancellationToken: CancellationToken): Promise<void> {
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
    const keyString: string | null = await generatePasswordString(txtPublicPart.value, cancellationToken);
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
    await run(CancellationToken.none);
}

export class PasswordComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLButtonElement {
        return btnTabPasswords;
    }
    getTabContent(): HTMLElement {
        return divTabPasswords;
    }
    onTabSelected(): void {
        storageOutputComponent.show();
        updatePasswordGenerationParameters();
    }

    init(): void {
        privatePartComponent.registerOnChanged(async () => await run(CancellationToken.none));

        // dafuq!?
        numOutputSizeRange.max = DEFAULT_LENGTH.toString();
        numOutputSizeRange.value = DEFAULT_LENGTH.toString();

        ui.setupFeedbackButton(btnClearPublicPart, onClearPublicPartButtonClick);
        ui.setupFeedbackButton(btnGeneratePublicPart, onGeneratePublicPartButtonClick);

        ui.setupViewButton(txtResultPassword, btnViewResultPassword);

        ui.setupCopyButton(txtPublicPart, btnCopyPublicPart);
        copyPasswordFunction = ui.setupCopyButton(txtResultPassword, btnCopyResultPassword);

        numOutputSizeRange.addEventListener('input', onOutputSizeRangeInput);
        numOutputSizeNum.addEventListener('input', onOutputSizeNumInput);

        txtAlphabet.addEventListener('input', onAlphabetTextInput);
        ui.setupFeedbackButton(btnResetAlphabet, onResetAlphabetButtonClick);

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
    }
};
