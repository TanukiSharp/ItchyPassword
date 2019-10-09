const txtPrivatePart = document.getElementById('txtPrivatePart');
const txtPrivatePartConfirmation = document.getElementById('txtPrivatePartConfirmation');
const txtPublicPart = document.getElementById('txtPublicPart');

const spnPrivatePartSize = document.getElementById('spnPrivatePartSize');
const spnPrivatePartSizeConfirmation = document.getElementById('spnPrivatePartSizeConfirmation');

const numOutputSizeRange = document.getElementById('numOutputSizeRange');
const numOutputSizeNum = document.getElementById('numOutputSizeNum');

const txtCustomAlphabet = document.getElementById('txtCustomAlphabet');
const btnResetAlphabet = document.getElementById('btnResetAlphabet');

const txtResultB16 = document.getElementById('txtResultB16');
const txtResultB64 = document.getElementById('txtResultB64');

const spnAlphabetSize = document.getElementById('spnAlphabetSize');
const txtResultCustomBase = document.getElementById('txtResultCustomBase');

const txtParameters = document.getElementById('txtParameters');

const defaultLength = 64;
// Alphabet v1 is screwed, the character { appears twice and } is missing.
const defaultAlphabetV1 = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:\'",<.>/?';
// Alphabet v2 is correct and in ASCII order.
const defaultAlphabetV2 = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

const defaultAlphabet = defaultAlphabetV2;

numOutputSizeRange.max = defaultLength;
numOutputSizeRange.value = defaultLength;

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
            tail
        };
    } else {
        chainInfo.tail[firstPath] = tail;
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

    const alphabet = txtCustomAlphabet.value;
    if (alphabet !== defaultAlphabet) {
        leaf.alphabet = alphabet;
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
    spnAlphabetSize.innerText = txtCustomAlphabet.value.length;
};

const updateAlphabetValidityDisplay = (isAlphabetValid) => {
    if (isAlphabetValid) {
        txtCustomAlphabet.style.removeProperty('background');
    } else {
        txtCustomAlphabet.style.setProperty('background', '#FFD0D0');
    }
};

txtCustomAlphabet.addEventListener('input', () => {
    const isAlphabetValidResult = isAlphabetValid(txtCustomAlphabet.value);

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
    txtResultB16.value = '';
    txtResultB64.value = '';
    txtResultCustomBase.value = '';
};

const canRun = () => {
    const alphabet = txtCustomAlphabet.value;

    if (isAlphabetValid(alphabet) === false) {
        return false;
    }

    if (txtPrivatePart.value.length <= 0 || txtPublicPart.value.length <= 0 || alphabet.length <= 1) {
        return false;
    }

    return true;
};

const run = async () => {
    checkPrivatePartsMatching();
    updateParameters();

    if (canRun() === false) {
        clearOutputs();
        return;
    }

    const keyBytes = await generatePassword(txtPrivatePart.value, txtPublicPart.value);

    txtResultB16.value = truncate(bufferToHexadeximal(keyBytes), numOutputSizeRange.value);
    txtResultB64.value = truncate(bufferToBase64(keyBytes), numOutputSizeRange.value);
    txtResultCustomBase.value = truncate(toCustomBase(keyBytes, txtCustomAlphabet.value), numOutputSizeRange.value);
};

const resetAlphabet = () => {
    txtCustomAlphabet.value = defaultAlphabet;
    updateAlphabetSize();

    const isAlphabetValidResult = isAlphabetValid(txtCustomAlphabet.value);

    updateAlphabetValidityDisplay(isAlphabetValidResult);

    if (isAlphabetValidResult) {
        run();
    }
};

txtPrivatePart.addEventListener('input', () => {
    spnPrivatePartSize.innerText = txtPrivatePart.value.length;
    run();
});

const checkPrivatePartsMatching = () => {
    if (txtPrivatePartConfirmation.value === txtPrivatePart.value) {
        txtPrivatePartConfirmation.style.setProperty('background', '#D0FFD0');
    } else {
        txtPrivatePartConfirmation.style.setProperty('background', '#FFD0D0');
    }
};

txtPrivatePartConfirmation.addEventListener('input', () => {
    spnPrivatePartSizeConfirmation.innerText = txtPrivatePartConfirmation.value.length;
    run();
});

txtPublicPart.addEventListener('input', () => {
    updateParameters();
    run();
});

updateOutputSizeRangeToNum();
resetAlphabet();
checkPrivatePartsMatching();
