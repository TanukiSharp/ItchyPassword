import { getElementById, setupCopyButton, ERROR_COLOR } from '../ui';
import * as privatePartComponent from './privatePartComponent';

import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import { PlainObject } from '../PlainObject';
import { PasswordGeneratorV1 } from '../passwordGenerators/v1';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

import * as storageOutputComponent from './storageOutputComponent';

const btnTabPasswords: HTMLInputElement = getElementById('btnTabPasswords');
const divTabPasswords: HTMLInputElement = getElementById('divTabPasswords');

const passwordGenerator: crypto.IPasswordGenerator = new PasswordGeneratorV1('Password');

const txtPublicPart: HTMLInputElement = getElementById('txtPublicPart');
const spnPublicPartSize: HTMLInputElement = getElementById('spnPublicPartSize');
const btnGeneratePublicPart: HTMLInputElement = getElementById('btnGeneratePublicPart');
const btnClearPublicPart: HTMLInputElement = getElementById('btnClearPublicPart');
const btnCopyPublicPart: HTMLInputElement = getElementById('btnCopyPublicPart');
const btnShowHidePasswordOptionalFeatures: HTMLInputElement = getElementById('btnShowHidePasswordOptionalFeatures');

const lblAlphabetLength: HTMLInputElement = getElementById('lblAlphabetLength');
const numOutputSizeRange: HTMLInputElement = getElementById('numOutputSizeRange');
const numOutputSizeNum: HTMLInputElement = getElementById('numOutputSizeNum');

const lblAlphabet: HTMLInputElement = getElementById('lblAlphabet');
const txtAlphabet: HTMLInputElement = getElementById('txtAlphabet');
const spnAlphabetSize: HTMLInputElement = getElementById('spnAlphabetSize');
const divPasswordAlphabetActions: HTMLInputElement = getElementById('divPasswordAlphabetActions');
const btnResetAlphabet: HTMLInputElement = getElementById('btnResetAlphabet');

const txtResultPassword: HTMLInputElement = getElementById('txtResultPassword');
const spnResultPasswordLength: HTMLInputElement = getElementById('spnResultPasswordLength');
const btnCopyResultPassword: HTMLInputElement = getElementById('btnCopyResultPassword');

const DEFAULT_LENGTH: number = 64;
const DEFAULT_ALPHABET: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const RESERVED_KEYS: string[] = ['alphabet', 'length', 'public', 'datetime'];

let passwordPublicPartLastChange: string | undefined;

function showHide(element: HTMLInputElement, isVisible: boolean) {
    if (isVisible) {
        element.style.removeProperty('display');
    } else {
        element.style.setProperty('display', 'none');
    }
}

function showHidePasswordOptionalFeatures(isVisible: boolean) {
    showHide(lblAlphabet, isVisible);
    showHide(txtAlphabet, isVisible);
    showHide(spnAlphabetSize, isVisible);
    showHide(divPasswordAlphabetActions, isVisible);

    showHide(lblAlphabetLength, isVisible);
    showHide(numOutputSizeRange, isVisible);
    showHide(numOutputSizeNum, isVisible);
}

function onClearPublicPartButtonClick(): void {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to clear the public part ?\nType \'y\' to accept', '') !== 'y') {
            return;
        }
    }

    txtPublicPart.value = '';
    updatePublicPartSize();

    updatePasswordPublicPartLastUpdate();
    updatePasswordGenerationParameters();
}

function onGeneratePublicPartButtonClick(): void {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to generate a new public part ?\nType \'y\' to accept', '') !== 'y') {
            return;
        }
    }

    const randomString: string = crypto.generateRandomString();
    txtPublicPart.value = randomString;
    updatePublicPartSize();

    updatePasswordPublicPartLastUpdate();

    run();
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

function setupViewButton(txt: HTMLInputElement, buttonName: string): void {
    const btn: HTMLInputElement = getElementById(buttonName);
    btn.addEventListener('click', () => {
        if (txt.type === 'password') {
            txt.type = 'input';
            btn.innerHTML = 'Hide';
        } else {
            txt.type = 'password';
            btn.innerHTML = 'View';
        }
    });
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
        txtAlphabet.style.setProperty('background', ERROR_COLOR);
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

async function onResetAlphabetButtonClick(): Promise<void> {
    resetAlphabet();
    updateAlphabetSize();
    await run();
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

export async function generatePasswordString(publicPart: string): Promise<string | null> {
    if (canRun(publicPart) === false) {
        return null;
    }

    const privatePartString: string = privatePartComponent.getPrivatePart();
    const privatePrivateBytes: ArrayBuffer = stringUtils.stringToArray(privatePartString);
    const publicPartBytes: ArrayBuffer = stringUtils.stringToArray(publicPart);
    const keyBytes: ArrayBuffer = await passwordGenerator.generatePassword(privatePrivateBytes, publicPartBytes);

    return arrayUtils.toCustomBaseOneWay(keyBytes, txtAlphabet.value);
}

async function run(): Promise<void> {
    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const keyString: string | null = await generatePasswordString(txtPublicPart.value);
    if (keyString === null) {
        return;
    }

    txtResultPassword.value = stringUtils.truncate(keyString, Math.max(4, parseInt(numOutputSizeRange.value, 10)));

    updateResultPasswordLength();

    updatePasswordGenerationParameters();
}

async function resetAlphabet(): Promise<void> {
    txtAlphabet.value = DEFAULT_ALPHABET;
    updateAlphabetSize();

    const isAlphabetValidResult: boolean = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult) {
        await run();
    }
}

async function onPublicPartTextInput(): Promise<void> {
    updatePublicPartSize();
    updatePasswordPublicPartLastUpdate();
    await run();
}

export class PasswordComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabPasswords;
    }
    getTabContent(): HTMLInputElement {
        return divTabPasswords;
    }
    onTabSelected(): void {
        storageOutputComponent.show();
        updatePasswordGenerationParameters();
    }

    init(): void {
        privatePartComponent.registerOnChanged(run);

        // dafuq!?
        numOutputSizeRange.max = DEFAULT_LENGTH.toString();
        numOutputSizeRange.value = DEFAULT_LENGTH.toString();

        btnClearPublicPart.addEventListener('click', onClearPublicPartButtonClick);
        btnGeneratePublicPart.addEventListener('click', onGeneratePublicPartButtonClick);

        setupViewButton(txtResultPassword, 'btnViewResultPassword');

        setupCopyButton(txtPublicPart, btnCopyPublicPart);
        setupCopyButton(txtResultPassword, btnCopyResultPassword);

        numOutputSizeRange.addEventListener('input', onOutputSizeRangeInput);
        numOutputSizeNum.addEventListener('input', onOutputSizeNumInput);

        txtAlphabet.addEventListener('input', onAlphabetTextInput);
        btnResetAlphabet.addEventListener('click', onResetAlphabetButtonClick);

        txtPublicPart.addEventListener('input', onPublicPartTextInput);

        let passwordOptionalFeaturesVisible = false;
        btnShowHidePasswordOptionalFeatures.addEventListener('click', function () {
            passwordOptionalFeaturesVisible = !passwordOptionalFeaturesVisible;
            showHidePasswordOptionalFeatures(passwordOptionalFeaturesVisible);
        });
        showHidePasswordOptionalFeatures(passwordOptionalFeaturesVisible);

        updatePublicPartSize();
        updateOutputSizeRangeToNum();
        resetAlphabet();
    }
};
