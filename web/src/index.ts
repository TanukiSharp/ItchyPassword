import { getElementById } from './ui';

import './components/privatePartComponent';
import './components/passwordComponent';
import './components/cipherComponent';
import './components/reEncryptComponent';

import { TabControl, ITabInfo } from './TabControl';

const btnTabNothing: HTMLInputElement = getElementById('btnTabNothing');
const btnTabPasswords: HTMLInputElement = getElementById('btnTabPasswords');
const btnTabCiphers: HTMLInputElement = getElementById('btnTabCiphers');
const btnTabReEncrypt: HTMLInputElement = getElementById('btnTabReEncrypt');

const divTabNothing: HTMLInputElement = getElementById('divTabNothing');
const divTabPasswords: HTMLInputElement = getElementById('divTabPasswords');
const divTabCiphers: HTMLInputElement = getElementById('divTabCiphers');
const divTabReEncrypt: HTMLInputElement = getElementById('divTabReEncrypt');

const tabs: ITabInfo[] = [
    { button: btnTabNothing, content: divTabNothing },
    { button: btnTabPasswords, content: divTabPasswords },
    { button: btnTabCiphers, content: divTabCiphers },
    { button: btnTabReEncrypt, content: divTabReEncrypt }
];

new TabControl(tabs);

declare const COMMITHASH: string;

const version = COMMITHASH.substr(0, 11);
const githubLink = '<a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>';

getElementById('divInfo').innerHTML = `${version}<br/>${githubLink}`;

import { toCustomBase, fromCustomBase } from './arrayUtils';
import { generateRandomBytes, generateRandomString, BASE62_ALPHABET } from './crypto';

for (let iteration: number = 0; iteration < 150; iteration += 1) {
    const original: ArrayBuffer = generateRandomBytes(64);
    const alphabet: string = generateRandomString(37, BASE62_ALPHABET);

    const encoded: string = toCustomBase(original, alphabet);
    const decoded: ArrayBuffer = fromCustomBase(encoded, alphabet);

    console.log('original:', original);
    console.log('decoded: ', decoded);

    const usableOriginal = new Uint8Array(original);
    const usableDecoded = new Uint8Array(decoded);

    if (usableOriginal.byteLength !== decoded.byteLength) {
        throw new Error();
    }

    for (let i: number = 0; i < usableOriginal.byteLength; i += 1) {
        if (usableOriginal[i] !== usableDecoded[i]) {
            console.log('usableOriginal:', usableOriginal);
            console.log('usableDecoded:', usableDecoded);
            throw new Error(`Error at iteration ${i}`);
        }
    }

    console.log(`Iteration ${iteration}`);
}
