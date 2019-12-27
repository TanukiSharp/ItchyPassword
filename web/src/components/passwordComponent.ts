import { getElementById, setupCopyButton, ERROR_COLOR } from '../ui';
import * as privatePart from './privatePartComponent';

import * as crypto from '../crypto';
import * as stringUtils from '../stringUtils';
import * as arrayUtils from '../arrayUtils';

import { PlainObject } from '../PlainObject';
import { PasswordGeneratorV1 } from '../passwordGenerators/v1';
import { ITabInfo } from '../TabControl';
import { IComponent } from './IComponent';

const btnTabPasswords: HTMLInputElement = getElementById('btnTabPasswords');
const divTabPasswords: HTMLInputElement = getElementById('divTabPasswords');

const passwordGenerator: crypto.IPasswordGenerator = new PasswordGeneratorV1('Password');

const txtPath: HTMLInputElement = getElementById('txtPath');
const txtPublicPart: HTMLInputElement = getElementById('txtPublicPart');
const btnGeneratePublicPart: HTMLInputElement = getElementById('btnGeneratePublicPart');
const btnClearPublicPart: HTMLInputElement = getElementById('btnClearPublicPart');
const btnCopyPublicPart: HTMLInputElement = getElementById('btnCopyPublicPart');
const spnCopyPublicPartFeedback: HTMLInputElement = getElementById('spnCopyPublicPartFeedback');

const numOutputSizeRange: HTMLInputElement = getElementById('numOutputSizeRange');
const numOutputSizeNum: HTMLInputElement = getElementById('numOutputSizeNum');

const txtAlphabet: HTMLInputElement = getElementById('txtAlphabet');
const spnAlphabetSize: HTMLInputElement = getElementById('spnAlphabetSize');
const btnResetAlphabet: HTMLInputElement = getElementById('btnResetAlphabet');

const txtResultPassword: HTMLInputElement = getElementById('txtResultPassword');
const spnResultPasswordLength: HTMLInputElement = getElementById('spnResultPasswordLength');
const btnCopyResultPassword: HTMLInputElement = getElementById('btnCopyResultPassword');
const spnCopyResultPasswordFeedback: HTMLInputElement = getElementById('spnCopyResultPasswordFeedback');

const txtParameters: HTMLInputElement = getElementById('txtParameters');
const txtCustomKeys: HTMLInputElement = getElementById('txtCustomKeys');

const DEFAULT_LENGTH: number = 64;
const DEFAULT_ALPHABET: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const RESERVED_KEYS: string[] = ['alphabet', 'length', 'public', 'datetime'];

let passwordPublicPartLastChange: string | undefined;

function onClearPublicPartButtonClick(): void {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to clear the public part ?\nType \'y\' to accept', '') !== 'y') {
            return;
        }
    }

    txtPublicPart.value = '';

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

function updateCustomKeysDisplay(isValid: boolean): void {
    if (isValid) {
        txtCustomKeys.style.removeProperty('background');
        return;
    }

    txtCustomKeys.style.setProperty('background', ERROR_COLOR);
}

function parseCustomKeys(): PlainObject | null {
    if (txtCustomKeys.value === '') {
        return {};
    }

    try {
        const obj: any = JSON.parse(txtCustomKeys.value);
        if (obj === null || obj.constructor.name !== 'Object') {
            return null;
        }
        return obj as PlainObject;
    } catch {
        return null;
    }
}

function shallowMerge(source: PlainObject | null, target: PlainObject | null): PlainObject {
    const result: PlainObject = {};

    if (source !== null) {
        for (const [key, value] of Object.entries(source)) {
            if (RESERVED_KEYS.includes(key) === false) {
                result[key] = value;
            }
        }
    }

    if (target !== null) {
        for (const [key, value] of Object.entries(target)) {
            result[key] = value;
        }
    }

    return result;
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

function updateResultPasswordLength() {
    spnResultPasswordLength.innerHTML = txtResultPassword.value.length.toString().padStart(2, ' ');
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

type IChainInfo = {
    head: PlainObject,
    tailParent: PlainObject,
    tail: PlainObject
};

// Transforms a path like "a/b/c/d" into a hierarchy of objects like { "a": { "b": { "c": { "d": {} } } } }
// From the result object, head is the root object that contains "a", tail is the value of "d", and tailParent is the value of "c"
function pathToObjectChain(path: string, chainInfo: IChainInfo | undefined = undefined): IChainInfo {
    const separatorIndex: number = path.indexOf('/');

    const tail: PlainObject = {};

    const firstPath: string = separatorIndex >= 0 ? path.substr(0, separatorIndex) : path;
    const remainingPath: string | undefined = separatorIndex >= 0 ? path.substr(separatorIndex + 1) : undefined;

    if (chainInfo === undefined) {
        const node: PlainObject = {};
        node[firstPath] = tail;
        chainInfo = {
            head: node,
            tailParent: node,
            tail
        };
    } else {
        chainInfo.tail[firstPath] = tail;
        chainInfo.tailParent = chainInfo.tail;
        chainInfo.tail = tail;
    }

    if (remainingPath) {
        return pathToObjectChain(remainingPath, chainInfo);
    }

    return chainInfo;
}

function updatePasswordGenerationParameters(): void {
    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const chainInfo: IChainInfo = pathToObjectChain(txtPath.value);
    const leaf: PlainObject = chainInfo.tail;

    leaf.public = txtPublicPart.value;
    leaf.datetime = passwordPublicPartLastChange;

    const numericValue: number = txtResultPassword.value.length;
    if (numericValue !== DEFAULT_LENGTH) {
        leaf.length = numericValue;
    }

    const alphabet: string = txtAlphabet.value;
    if (alphabet !== DEFAULT_ALPHABET) {
        leaf.alphabet = alphabet;
    }

    const customKeys: PlainObject | null = parseCustomKeys();
    updateCustomKeysDisplay(customKeys !== null);
    const resultParameters: PlainObject = shallowMerge(customKeys, leaf);

    if (Object.keys(resultParameters).length === 0) {
        // Set the value of the first (single) property of the object to null.
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = null;
    } else {
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = resultParameters;
    }

    txtParameters.value = JSON.stringify(chainInfo.head, undefined, 4);
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

function updateAlphabetSize(): void {
    spnAlphabetSize.innerHTML = txtAlphabet.value.length.toString();

    const alphabetSizeDigitCount: number = txtAlphabet.value.length.toString().length;
    if (alphabetSizeDigitCount < 2) {
        // Add a space to keep a nice visual alignment.
        spnAlphabetSize.innerHTML = spnAlphabetSize.innerHTML.padStart(2, ' ');
    }
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
    txtParameters.value = '';

    updateResultPasswordLength();
}

function canRun(): boolean {
    const alphabet: string = txtAlphabet.value;

    if (isAlphabetValid(alphabet) === false) {
        return false;
    }

    if (privatePart.getPrivatePart().length <= 0 || txtPublicPart.value.length < 8 || alphabet.length < 2) {
        return false;
    }

    return true;
}

async function run() {
    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const privatePartString: string = privatePart.getPrivatePart();
    const publicPartString = txtPublicPart.value;

    const privatePrivateBytes: ArrayBuffer = stringUtils.stringToArray(privatePartString);
    const publicPartBytes: ArrayBuffer = stringUtils.stringToArray(publicPartString);

    const keyBytes: ArrayBuffer = await passwordGenerator.generatePassword(privatePrivateBytes, publicPartBytes);

    const keyString: string = arrayUtils.toCustomBaseOneWay(keyBytes, txtAlphabet.value);
    txtResultPassword.value = stringUtils.truncate(keyString, Math.max(4, parseInt(numOutputSizeRange.value, 10)));

    updateResultPasswordLength();

    updatePasswordGenerationParameters();
}

async function resetAlphabet() {
    txtAlphabet.value = DEFAULT_ALPHABET;
    updateAlphabetSize();

    const isAlphabetValidResult: boolean = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult) {
        await run();
    }
}

function onPathTextInput() {
    updatePasswordGenerationParameters();
}

async function onPublicPartTextInput(): Promise<void> {
    updatePasswordPublicPartLastUpdate();
    await run();
}

function onCustomKeysTextInput(): void {
    updatePasswordGenerationParameters();
}

export class PasswordComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabPasswords;
    }
    getTabContent(): HTMLInputElement {
        return divTabPasswords;
    }
    onTabSelected(): void {
    }

    init(): void {
        privatePart.registerOnChanged(run);

        // dafuq!?
        numOutputSizeRange.max = DEFAULT_LENGTH.toString();
        numOutputSizeRange.value = DEFAULT_LENGTH.toString();

        btnClearPublicPart.addEventListener('click', onClearPublicPartButtonClick);
        btnGeneratePublicPart.addEventListener('click', onGeneratePublicPartButtonClick);

        setupViewButton(txtResultPassword, 'btnViewResultPassword');

        setupCopyButton(txtPublicPart, btnCopyPublicPart, spnCopyPublicPartFeedback);
        setupCopyButton(txtResultPassword, btnCopyResultPassword, spnCopyResultPasswordFeedback);

        numOutputSizeRange.addEventListener('input', onOutputSizeRangeInput);
        numOutputSizeNum.addEventListener('input', onOutputSizeNumInput);

        txtAlphabet.addEventListener('input', onAlphabetTextInput);
        btnResetAlphabet.addEventListener('click', onResetAlphabetButtonClick);

        txtPath.addEventListener('input', onPathTextInput);

        txtPublicPart.addEventListener('input', onPublicPartTextInput);

        txtCustomKeys.addEventListener('input', onCustomKeysTextInput);

        updateOutputSizeRangeToNum();
        resetAlphabet();
    }
};
