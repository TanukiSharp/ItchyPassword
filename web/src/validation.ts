import * as crypto from './crypto';
import * as arrayUtils from './arrayUtils';
import * as stringUtils from './stringUtils';

import { CancellationToken } from './asyncUtils';

import { PasswordGeneratorV1 } from './passwordGenerators/v1';
import { CipherV2 } from './ciphers/v2';

const defaultAlphabet: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

interface IValidationResponse {
    generatedPassword: string,
    backendClear: string,
    backendEncrypted: string
}

async function postData(url: string = '', data: object = {}): Promise<string> {
    const response: Response = await fetch(url, {
        method: 'POST',
        cache: 'no-cache',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });

    if (response.status < 200 || response.status > 299) {
        throw new Error(`Error ${response.status}`);
    }

    return response.text() as Promise<string>;
}

async function validate(): Promise<void> {
    const passwordGenerator: crypto.IPasswordGenerator = new PasswordGeneratorV1('Password');
    const cipher: crypto.ICipher = new CipherV2();

    for (let i: number = 0; i < 10; i += 1) {
        const privatePartBytes: ArrayBuffer = crypto.generateRandomBytes(64);
        const publicPartBytes: ArrayBuffer = crypto.generateRandomBytes(64);
        const generatedPasswordBytes: ArrayBuffer = await passwordGenerator.generatePassword(privatePartBytes, publicPartBytes, CancellationToken.none);
        const frontendClearBytes: ArrayBuffer = crypto.generateRandomBytes(64);
        const frontendEncryptedBytes: ArrayBuffer = await cipher.encrypt(frontendClearBytes, generatedPasswordBytes, CancellationToken.none);

        const responseContent: string = await postData('http://localhost:5000/', {
            privatePart: arrayUtils.toBase16(privatePartBytes),
            publicPart: arrayUtils.toBase16(publicPartBytes),
            alphabet: defaultAlphabet,
            frontendClear: arrayUtils.toBase16(frontendClearBytes),
            frontendEncrypted: arrayUtils.toBase16(frontendEncryptedBytes)
        });

        const backendData: IValidationResponse = JSON.parse(responseContent);

        const frontendGeneratedPassword: string = arrayUtils.toCustomBaseOneWay(generatedPasswordBytes, defaultAlphabet);

        if (frontendGeneratedPassword !== backendData.generatedPassword) {
            throw new Error(`Passwords mismatch at test ${i}, frontend: ${frontendGeneratedPassword}, backend: ${backendData.generatedPassword}`);
        }

        const backendEncryptedBytes: ArrayBuffer = stringUtils.fromBase16(backendData.backendEncrypted);
        const decryptedBackendEncryptedBytes: ArrayBuffer = await cipher.decrypt(backendEncryptedBytes, generatedPasswordBytes, CancellationToken.none);

        const decryptedBackendEncrypted: string = arrayUtils.toBase16(decryptedBackendEncryptedBytes);
        if (decryptedBackendEncrypted !== backendData.backendClear) {
            throw new Error(`Failed to decrypt, expected '${backendData.backendClear}', computed '${decryptedBackendEncrypted}'`);
        }
    }
};

export async function startValidation(): Promise<void> {
    // const privatePartBytes: ArrayBuffer = crypto.generateRandomBytes(64);
    // const publicPartBytes: ArrayBuffer = crypto.generateRandomBytes(64);
    // const passwordBytes: ArrayBuffer = await crypto.generatePassword(privatePartBytes, publicPartBytes, 'Password');

    // const privatePart: string = arrayUtils.toBase16(privatePartBytes);
    // const publicPart: string = arrayUtils.toBase16(publicPartBytes);
    // const password: string = arrayUtils.toBase16(passwordBytes);

    // console.log(`privatePart: ${privatePart}`);
    // console.log(`publicPart: ${publicPart}`);
    // console.log(`password: ${password}`);

    // ************************************************************************

    // const privateBytes: ArrayBuffer = stringUtils.fromBase16('1c7fb7cc279123eb651ddce91ef7031decd45e6822944c77d63b615d90ba355ccf249256a280ef2c6c5d7a27bc5ceec673a066aecd8ee92032c95de4997d1b55');
    // const publicBytes: ArrayBuffer = stringUtils.fromBase16('c92aaa8a0d5c6db39738a0721e1fa4351cb7bbc625cf27eb43df65bd861c924862f67a53c262fffa8ef392326b963f4be04614e8420035a4683ca75dd322745d');
    // const generatedPassword: string = arrayUtils.toBase16(await crypto.generatePassword(privateBytes, publicBytes, 'Password'));

    // if (generatedPassword !== 'c3cd8ab510b071cfc5cab40c5e060d6b910ba8b528e4c2f85fa22d9a96360f076ce541ba5468c6c9edac610861d3a4745ccd06abf739c8cda9292ad6471b3073') {
    //     throw new Error();
    // }

    // console.log(generatedPassword);

    // ************************************************************************

    // const input: ArrayBuffer = new Uint8Array([51, 2, 33, 42, 51, 6, 7, 8, 9, 101, 255, 12, 13, 14, 15, 61, 17, 19, 81]);
    // const password: ArrayBuffer = new Uint8Array([201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216]);
    // const encrypted: ArrayBuffer = await crypto.encrypt(input, password);
    // console.log('encrypted:', new Uint8Array(encrypted));

    // const decrypted: ArrayBuffer = await crypto.decrypt(encrypted, password);
    // console.log('decrypted:', new Uint8Array(decrypted));

    // ************************************************************************

    console.log('Validating...');
    const start: number = window.performance.now();
    await validate();
    const duration: number = window.performance.now() - start;
    console.log(`Validated (took ${duration / 1000} seconds)`);
}

startValidation();
