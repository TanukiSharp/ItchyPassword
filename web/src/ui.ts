import * as crypto from './crypto';
import * as stringUtils from './stringUtils';
import * as arrayUtils from './arrayUtils';

import VisualFeedback from './VisualFeedback';
import TimedAction from './TimedAction';
import { PlainObject } from './PlainObject';

function getElementById(elementName: string): HTMLInputElement {
    const element: HTMLElement|null = document.getElementById(elementName);

    if (elementName === null) {
        throw new Error(`DOM element '${elementName}' not found.`);
    }

    return element as HTMLInputElement;
}

const txtPrivatePart: HTMLInputElement = getElementById('txtPrivatePart');
const txtPrivatePartConfirmation: HTMLInputElement = getElementById('txtPrivatePartConfirmation');
const btnProtect: HTMLInputElement = getElementById('btnProtect');
const btnClearProtected: HTMLInputElement = getElementById('btnClearProtected');
const spnProtectedConfirmation: HTMLInputElement = getElementById('spnProtectedConfirmation');
const txtPath: HTMLInputElement = getElementById('txtPath');
const txtPublicPart: HTMLInputElement = getElementById('txtPublicPart');
const btnGeneratePublicPart: HTMLInputElement = getElementById('btnGeneratePublicPart');

const spnPrivatePartSize: HTMLInputElement = getElementById('spnPrivatePartSize');
const spnPrivatePartSizeConfirmation: HTMLInputElement = getElementById('spnPrivatePartSizeConfirmation');

const numOutputSizeRange: HTMLInputElement = getElementById('numOutputSizeRange');
const numOutputSizeNum: HTMLInputElement = getElementById('numOutputSizeNum');

const txtAlphabet: HTMLInputElement = getElementById('txtAlphabet');
const spnAlphabetSize: HTMLInputElement = getElementById('spnAlphabetSize');
const btnResetAlphabet: HTMLInputElement = getElementById('btnResetAlphabet');

const txtResultPassword: HTMLInputElement = getElementById('txtResultPassword');

const spnResultPasswordLength: HTMLInputElement = getElementById('spnResultPasswordLength');

const spnCopyResultPasswordFeedback: HTMLInputElement = getElementById('spnCopyResultPasswordFeedback');

const txtParameters: HTMLInputElement = getElementById('txtParameters');
const txtCustomKeys: HTMLInputElement = getElementById('txtCustomKeys');

const DEFAULT_LENGTH: number = 64;

// Alphabet v1 is screwed, the character { appears twice and } is missing.
const DEFAULT_ALPHABET_V1: string = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';
// Alphabet v2 is correct and in ASCII order.
const DEFAULT_ALPHABET_V2: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const DEFAULT_ALPHABET: string = DEFAULT_ALPHABET_V2;

const PRIVATE_PART_PROTECTION_TIMEOUT: number = 60 * 1000;

const SUCCESS_COLOR: string = '#D0FFD0';
const ERROR_COLOR: string = '#FFD0D0';

const RESERVED_KEYS: string[] = ['alphabet', 'length', 'public'];

// dafuq!?
numOutputSizeRange.max = DEFAULT_LENGTH.toString();
numOutputSizeRange.value = DEFAULT_LENGTH.toString();

let privatePart: string | undefined;

btnGeneratePublicPart.addEventListener('click', () => {
    const randomString: string = crypto.generateRandomString();
    txtPublicPart.value = randomString;
    run();
});

function getPrivatePart(): string {
    if (privatePart !== undefined) {
        return privatePart;
    }
    return txtPrivatePart.value;
}

function protectPrivatePart(): void {
    if (txtPrivatePart.value.length === 0) {
        return;
    }

    privatePart = txtPrivatePart.value;
    spnProtectedConfirmation.innerHTML = `Protected, ${privatePart.length} characters`;

    txtPrivatePart.value = '';
    txtPrivatePartConfirmation.value = '';
    spnPrivatePartSize.innerHTML = '0';
    spnPrivatePartSizeConfirmation.innerHTML = '0';

    updatePrivatePartsMatching();
}

btnProtect.addEventListener('click', () => {
    protectPrivatePart();
});

function clearPrivatePartProtection() {
    privatePart = undefined;
    spnProtectedConfirmation.innerHTML = '';
}

btnClearProtected.addEventListener('click', () => {
    clearPrivatePartProtection();
    run();
});

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

const copyToClipboardFeedbackObject: VisualFeedback = new VisualFeedback(spnCopyResultPasswordFeedback);

async function writeToClipboard(text: string): Promise<boolean> {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (error) {
        console.error(error.stack || error);
        return false;
    }
}

function setupCopyButton(txt: HTMLInputElement, buttonName: string): void {
    const btn: HTMLInputElement = getElementById(buttonName);
    btn.addEventListener('click', async () => {
        if (await writeToClipboard(txt.value)) {
            copyToClipboardFeedbackObject.setText('Copied', 3000);
        } else {
            copyToClipboardFeedbackObject.setText('<span style="color: red">Failed to copy</span>', 3000);
        }
    });
}

function updateResultPasswordLength() {
    spnResultPasswordLength.innerHTML = txtResultPassword.value.length.toString().padStart(2, ' ');
}

setupViewButton(txtResultPassword, 'btnViewResultPassword');

setupCopyButton(txtResultPassword, 'btnCopyResultPassword');

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
        txtParameters.value = '';
        return;
    }

    const chainInfo: IChainInfo = pathToObjectChain(txtPath.value);
    const leaf: PlainObject = chainInfo.tail;

    leaf.public = txtPublicPart.value;

    const numericValue: number = parseInt(numOutputSizeNum.value, 10);
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

function updateOutputSizeNumToRange(): void {
    const min: number = parseInt(numOutputSizeRange.min, 10);
    const val: number = parseInt(numOutputSizeNum.value, 10);
    const max: number = parseInt(numOutputSizeRange.max, 10);
    numOutputSizeRange.value = Math.max(min, Math.min(val, max)).toString();
}

numOutputSizeRange.addEventListener('input', () => {
    updateOutputSizeRangeToNum();
    run();
});

numOutputSizeNum.addEventListener('input', () => {
    updateOutputSizeNumToRange();
    updateOutputSizeRangeToNum();
    run();
});

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

txtAlphabet.addEventListener('input', () => {
    const isAlphabetValidResult: boolean = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult === false) {
        return;
    }

    updateAlphabetSize();
    run();
});

btnResetAlphabet.addEventListener('click', () => {
    resetAlphabet();
    updateAlphabetSize();
    run();
});

function clearOutputs(): void {
    txtResultPassword.value = '';

    updateResultPasswordLength();
}

function canRun(): boolean {
    const alphabet: string = txtAlphabet.value;

    if (isAlphabetValid(alphabet) === false) {
        return false;
    }

    if (getPrivatePart().length <= 0 || txtPublicPart.value.length < 8 || alphabet.length < 2) {
        return false;
    }

    return true;
}

async function run() {
    updatePasswordGenerationParameters();

    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const keyBytes: ArrayBuffer = await crypto.generatePassword(getPrivatePart(), txtPublicPart.value);

    const keyString: string = arrayUtils.toCustomBase(keyBytes, txtAlphabet.value);
    txtResultPassword.value = stringUtils.truncate(keyString, parseInt(numOutputSizeRange.value, 10));

    updateResultPasswordLength();
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

const protectPrivatePartAction: TimedAction = new TimedAction(protectPrivatePart, PRIVATE_PART_PROTECTION_TIMEOUT);

txtPrivatePart.addEventListener('input', () => {
    clearPrivatePartProtection();
    spnPrivatePartSize.innerHTML = txtPrivatePart.value.length.toString();
    updatePrivatePartsMatching();
    run();
    protectPrivatePartAction.reset();
});

function updatePrivatePartsMatching(): void {
    if (txtPrivatePartConfirmation.value === txtPrivatePart.value) {
        txtPrivatePartConfirmation.style.setProperty('background', SUCCESS_COLOR);
    } else {
        txtPrivatePartConfirmation.style.setProperty('background', ERROR_COLOR);
    }
};

txtPrivatePartConfirmation.addEventListener('input', () => {
    spnPrivatePartSizeConfirmation.innerHTML = txtPrivatePartConfirmation.value.length.toString();
    updatePrivatePartsMatching();
});

txtPath.addEventListener('input', () => {
    updatePasswordGenerationParameters();
});

txtPublicPart.addEventListener('input', () => {
    updatePasswordGenerationParameters();
    run();
});

txtCustomKeys.addEventListener('input', () => {
    updatePasswordGenerationParameters();
});

updateOutputSizeRangeToNum();
resetAlphabet();
updatePrivatePartsMatching();
