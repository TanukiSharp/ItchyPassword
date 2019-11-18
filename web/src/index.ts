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
