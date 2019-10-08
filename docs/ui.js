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
});

const updateAlphabetSize = () => {
    spnAlphabetSize.innerText = txtCustomAlphabet.value.length;
};

txtCustomAlphabet.addEventListener('input', () => {
    updateAlphabetSize();
    run();
});

const resetAlphabet = () => {
    txtCustomAlphabet.value = defaultCustomAlphabet;
    updateAlphabetSize();
};

btnResetAlphabet.addEventListener('click', () => {
    resetAlphabet();
    updateAlphabetSize();
});

updateOutputSizeRangeToNum();
resetAlphabet();

const clearOutputs = () => {
    txtResultB16.value = '';
    txtResultB64.value = '';
    txtResultCustomBase.value = '';
};

const run = async () => {
    const privatePart = txtPrivatePart.value;
    const publicPart = txtPublicPart.value;

    checkPrivatePartsMatching();

    if (privatePart.length <= 0 || publicPart.length <= 0) {
        clearOutputs();
        return;
    }

    const keyBytes = await generatePassword(privatePart, publicPart);

    txtResultB16.value = truncate(bufferToHexadeximal(keyBytes), numOutputSizeRange.value);
    txtResultB64.value = truncate(bufferToBase64(keyBytes), numOutputSizeRange.value);
    txtResultCustomBase.value = truncate(toCustomBase(keyBytes, txtCustomAlphabet.value), numOutputSizeRange.value);
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

checkPrivatePartsMatching();

txtPublicPart.addEventListener('input', run);
