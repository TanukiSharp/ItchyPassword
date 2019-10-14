const txtPrivatePart = document.getElementById('txtPrivatePart');
const txtPrivatePartConfirmation = document.getElementById('txtPrivatePartConfirmation');
const txtPublicPart = document.getElementById('txtPublicPart');
const txtPublicPartConfirmation = document.getElementById('txtPublicPartConfirmation');

const spnPrivatePartSize = document.getElementById('spnPrivatePartSize');
const spnPrivatePartSizeConfirmation = document.getElementById('spnPrivatePartSizeConfirmation');

const numOutputSizeRange = document.getElementById('numOutputSizeRange');
const numOutputSizeNum = document.getElementById('numOutputSizeNum');

const txtAlphabet = document.getElementById('txtAlphabet');
const spnAlphabetSize = document.getElementById('spnAlphabetSize');
const btnResetAlphabet = document.getElementById('btnResetAlphabet');

const txtResultPassword = document.getElementById('txtResultPassword');

const spnResultPasswordLength = document.getElementById('spnResultPasswordLength');

const spnCopyResultPasswordFeedback = document.getElementById('spnCopyResultPasswordFeedback');

const txtParameters = document.getElementById('txtParameters');
const txtCustomKeys = document.getElementById('txtCustomKeys');

const defaultLength = 64;

// Alphabet v1 is screwed, the character { appears twice and } is missing.
const defaultAlphabetV1 = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';
// Alphabet v2 is correct and in ASCII order.
const defaultAlphabetV2 = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const defaultAlphabet = defaultAlphabetV2;

numOutputSizeRange.max = defaultLength;
numOutputSizeRange.value = defaultLength;

const SUCCESS_COLOR = '#D0FFD0';
const ERROR_COLOR = '#FFD0D0';

const RESERVED_KEYS = ['alphabet', 'length'];

const updateCustomKeysDisplay = (isValid) => {
    if (isValid) {
        txtCustomKeys.style.removeProperty('background');
        return;
    }

    txtCustomKeys.style.setProperty('background', ERROR_COLOR);
};

const parseCustomKeys = () => {
    if (txtCustomKeys.value === '') {
        return {};
    }

    try {
        const customKeys = JSON.parse(txtCustomKeys.value);
        return customKeys;
    } catch {
        return undefined;
    }
};

const shallowMerge = (source, target) => {
    const result = {};

    if (source === undefined || source === null || source.constructor.name !== 'Object') {
        return target;
    }

    for (const [key, value] of Object.entries(source)) {
        if (!RESERVED_KEYS.includes(key)) {
            result[key] = value;
        }
    }

    for (const [key, value] of Object.entries(target)) {
        result[key] = value;
    }

    return result;
};

const deepMerge = (source, target) => {
    for (const sourceKey of Object.keys(source)) {
        const targetValue = target[sourceKey];
        const sourceValue = source[sourceKey];

        if (targetValue === undefined ||
            targetValue === null ||
            targetValue.constructor.name !== 'Object' ||
            sourceValue.constructor.name !== 'Object') {
            target[sourceKey] = sourceValue;
            continue;
        }

        deepMerge(sourceValue, targetValue);
    }
};

const setupViewButton = (txt, buttonName) => {
    const btn = document.getElementById(buttonName);
    btn.addEventListener('click', () => {
        if (txt.type === 'password') {
            txt.type = 'input';
            btn.innerText = 'Hide';
        } else {
            txt.type = 'password';
            btn.innerText = 'View';
        }
    });
};

const createFeedbackObject = (element) => {
    const obj = {
        element,
        timeout: null,
        setText: (text, duration) => {
            element.innerHTML = text;
            if (this.timeout) {
                clearTimeout(this.timeout);
            }
            this.timeout = setTimeout(() => element.innerHTML = '', duration);
        }
    };

    return obj;
};

const copyToClipboardFeedbackObject = createFeedbackObject(spnCopyResultPasswordFeedback);

const writeToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (error) {
        console.error(error.stack || error);
        return false;
    }
};

const setupCopyButton = (txt, buttonName) => {
    const btn = document.getElementById(buttonName);
    btn.addEventListener('click', async () => {
        if (await writeToClipboard(txt.value)) {
            copyToClipboardFeedbackObject.setText('Copied', 3000);
        } else {
            copyToClipboardFeedbackObject.setText('<span style="color: red">Failed to copy</span>', 3000);
        }
    });
};

const updateResultPasswordLength = () => {
    spnResultPasswordLength.innerText = txtResultPassword.value.length.toString().padStart(2, ' ');
};

setupViewButton(txtResultPassword, 'btnViewResultPassword');

setupCopyButton(txtResultPassword, 'btnCopyResultPassword');

const isAlphabetValid = (alphabet) => {
    const sortedAlphabet = alphabet.split('');
    sortedAlphabet.sort();

    for (let i = 1; i < sortedAlphabet.length; i += 1) {
        if (sortedAlphabet[i - 1] === sortedAlphabet[i]) {
            return false;
        }
    }

    return true;
};

// Transforms a path like "a/b/c/d" into a hierarchy of objects like { "a": { "b": { "c": { "d": {} } } } }
// From the result object, head is the root object that contains "a", tail is the value of "d", and tailParent is the value of "c"
const pathToObjectChain = (path, chainInfo) => {
    const separatorIndex = path.indexOf('/');

    const tail = {};

    const firstPath = separatorIndex >= 0 ? path.substr(0, separatorIndex) : path;
    const remainingPath = separatorIndex >= 0 ? path.substr(separatorIndex + 1) : undefined;

    if (chainInfo === undefined) {
        const node = {};
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
};

const updateParameters = () => {
    if (canRun() === false) {
        txtParameters.value = '';
        return;
    }

    const chainInfo = pathToObjectChain(txtPublicPart.value);
    const leaf = chainInfo.tail;

    const numericValue = parseInt(numOutputSizeNum.value, 10);
    if (numericValue !== defaultLength) {
        leaf.length = numericValue;
    }

    const alphabet = txtAlphabet.value;
    if (alphabet !== defaultAlphabet) {
        leaf.alphabet = alphabet;
    }

    const customKeys = parseCustomKeys();
    updateCustomKeysDisplay(customKeys !== undefined);
    const resultParameters = shallowMerge(customKeys, leaf);

    if (Object.keys(resultParameters).length === 0) {
        // Set the value of the first (single) property of the object to null.
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = null;
    } else {
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = resultParameters;
    }

    txtParameters.value = JSON.stringify(chainInfo.head, undefined, 4);
};

const updateOutputSizeRangeToNum = () => {
    numOutputSizeNum.value = numOutputSizeRange.value;
};

const updateOutputSizeNumToRange = () => {
    numOutputSizeRange.value = Math.max(numOutputSizeRange.min, Math.min(numOutputSizeNum.value, numOutputSizeRange.max));
};

numOutputSizeRange.addEventListener('input', () => {
    updateOutputSizeRangeToNum();
    run();
});

numOutputSizeNum.addEventListener('input', () => {
    updateOutputSizeNumToRange();
    updateOutputSizeRangeToNum();
    run();
});

const updateAlphabetSize = () => {
    spnAlphabetSize.innerHTML = txtAlphabet.value.length;

    const alphabetSizeDigitCount = txtAlphabet.value.length.toString().length;
    if (alphabetSizeDigitCount < 2) {
        // Add a space to keep a nice visual alignment.
        spnAlphabetSize.innerHTML = spnAlphabetSize.innerHTML.padStart(2, ' ');
    }
};

const updateAlphabetValidityDisplay = (isAlphabetValid) => {
    if (isAlphabetValid) {
        txtAlphabet.style.removeProperty('background');
    } else {
        txtAlphabet.style.setProperty('background', ERROR_COLOR);
    }
};

txtAlphabet.addEventListener('input', () => {
    const isAlphabetValidResult = isAlphabetValid(txtAlphabet.value);

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

const clearOutputs = () => {
    txtResultPassword.value = '';

    updateResultPasswordLength();
};

const canRun = () => {
    const alphabet = txtAlphabet.value;

    if (isAlphabetValid(alphabet) === false) {
        return false;
    }

    if (txtPrivatePart.value.length <= 0 || txtPublicPart.value.length <= 0 || alphabet.length <= 1) {
        return false;
    }

    return true;
};

const run = async () => {
    updateParameters();

    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const keyBytes = await generatePassword(txtPrivatePart.value, txtPublicPart.value);

    txtResultPassword.value = truncate(toCustomBase(keyBytes, txtAlphabet.value), numOutputSizeRange.value);

    updateResultPasswordLength();
};

const resetAlphabet = () => {
    txtAlphabet.value = defaultAlphabet;
    updateAlphabetSize();

    const isAlphabetValidResult = isAlphabetValid(txtAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult) {
        run();
    }
};

txtPrivatePart.addEventListener('input', () => {
    spnPrivatePartSize.innerText = txtPrivatePart.value.length;
    checkPrivatePartsMatching();
    run();
});

const checkPrivatePartsMatching = () => {
    if (txtPrivatePartConfirmation.value === txtPrivatePart.value) {
        txtPrivatePartConfirmation.style.setProperty('background', SUCCESS_COLOR);
    } else {
        txtPrivatePartConfirmation.style.setProperty('background', ERROR_COLOR);
    }
};

const checkPublicPartsMatching = () => {
    if (txtPublicPartConfirmation.value === txtPublicPart.value) {
        txtPublicPartConfirmation.style.setProperty('background', SUCCESS_COLOR);
    } else {
        txtPublicPartConfirmation.style.setProperty('background', ERROR_COLOR);
    }
};

txtPrivatePartConfirmation.addEventListener('input', () => {
    spnPrivatePartSizeConfirmation.innerText = txtPrivatePartConfirmation.value.length;
    checkPrivatePartsMatching();
});

txtPublicPartConfirmation.addEventListener('input', () => {
    checkPublicPartsMatching();
});

txtPublicPart.addEventListener('input', () => {
    checkPublicPartsMatching();
    updateParameters();
    run();
});

txtCustomKeys.addEventListener('input', () => {
    updateParameters();
});

updateOutputSizeRangeToNum();
resetAlphabet();
checkPrivatePartsMatching();
checkPublicPartsMatching();
