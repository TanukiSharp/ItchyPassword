import { getElementById } from './ui';

import './PrivatePartComponent';
import './PasswordComponent';
import './CipherComponent';

import { TabControl, ITabInfo } from './TabControl';

const btnTabNothing: HTMLInputElement = getElementById('btnTabNothing');
const btnTabPasswords: HTMLInputElement = getElementById('btnTabPasswords');
const btnTabCiphers: HTMLInputElement = getElementById('btnTabCiphers');
const divTabNothing: HTMLInputElement = getElementById('divTabNothing');
const divTabPasswords: HTMLInputElement = getElementById('divTabPasswords');
const divTabCiphers: HTMLInputElement = getElementById('divTabCiphers');

const tabs: ITabInfo[] = [
    { button: btnTabNothing, content: divTabNothing },
    { button: btnTabPasswords, content: divTabPasswords },
    { button: btnTabCiphers, content: divTabCiphers }
];

new TabControl(tabs);
