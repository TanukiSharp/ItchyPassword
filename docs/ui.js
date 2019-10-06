const txtPrivatePart = document.getElementById('txtPrivatePart');
const txtPublicPart = document.getElementById('txtPublicPart');

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

const run = async () => {
    if (txtPublicPart.value.length <= 0) {
        return;
    }

    const keyBytes = await generatePassword(txtPrivatePart.value, txtPublicPart.value);

    txtResultB16.value = truncate(bufferToHexadeximal(keyBytes), numOutputSizeRange.value);
    txtResultB64.value = truncate(bufferToBase64(keyBytes), numOutputSizeRange.value);
    txtResultCustomBase.value = truncate(toCustomBase(keyBytes, txtCustomAlphabet.value), numOutputSizeRange.value);
};

txtPrivatePart.addEventListener('input', run);
txtPublicPart.addEventListener('input', run);
