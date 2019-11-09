import * as crypto from './crypto';
import * as stringUtils from './stringUtils';
import * as arrayUtils from './arrayUtils';

import { getElementById } from './ui';
import { getPrivatePart } from './PrivatePartComponent';

import { CipherV1 } from './ciphers/v1';

const cipher: crypto.ICipher = new CipherV1();

const txtCipherSource: HTMLInputElement = getElementById('txtCipherSource');
const txtCipherTarget: HTMLInputElement = getElementById('txtCipherTarget');
const btnEncrypt: HTMLInputElement = getElementById('btnEncrypt');
const btnDecrypt: HTMLInputElement = getElementById('btnDecrypt');

btnEncrypt.addEventListener('click', async () => {
    if (txtCipherSource.value.length === 0) {
        return;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        return;
    }

    const input: ArrayBuffer = stringUtils.stringToArray(txtCipherSource.value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const encrypted: ArrayBuffer = await cipher.encrypt(input, password);

    txtCipherTarget.value = arrayUtils.toBase16(encrypted);
});

btnDecrypt.addEventListener('click', async () => {
    if (txtCipherSource.value.length === 0) {
        return;
    }

    const privatePart: string = getPrivatePart();
    if (privatePart.length === 0) {
        return;
    }

    const input: ArrayBuffer = stringUtils.fromBase16(txtCipherSource.value);
    const password: ArrayBuffer = stringUtils.stringToArray(privatePart);

    const decrypted: ArrayBuffer = await cipher.decrypt(input, password);

    txtCipherTarget.value = arrayUtils.arrayToString(decrypted);
});
