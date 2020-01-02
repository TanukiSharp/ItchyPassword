import { getElementById } from '../ui';

import { IComponent } from './IComponent';
import { ITabInfo } from '../TabControl';

import * as passwordComponent from './passwordComponent';
import * as cipherComponent from './cipherComponent';
import * as storageOutputComponent from './storageOutputComponent';

import { IVaultStorage } from '../storages/IVaultStorage';
import { GitHubVaultStorage, IKeyValueStorage } from '../storages/GitHubVaultStorage';

const divTabVault: HTMLInputElement = getElementById('divTabVault');
const btnTabVault: HTMLInputElement = getElementById('btnTabVault');

const txtVault: HTMLInputElement = getElementById('txtVault');
const btnRefreshVault: HTMLInputElement = getElementById('btnRefreshVault');
const btnClearVaultSettings: HTMLInputElement = getElementById('btnClearVaultSettings');

let vaultStorage: IVaultStorage | null = null;

const LOCAL_STORAGE_KEY_USERNAME: string = 'ItchyPassword.Vault.Username';
const LOCAL_STORAGE_KEY_PASSWORD_PUBLIC: string = 'ItchyPassword.Vault.PasswordPublicPart';
const LOCAL_STORAGE_KEY_PASSWORD_LENGTH: string = 'ItchyPassword.Vault.PasswordLength';
const LOCAL_STORAGE_KEY_REPO: string = 'ItchyPassword.Vault.Repository';
const LOCAL_STORAGE_KEY_FILENAME: string = 'ItchyPassword.Vault.Filename';

class SecureLocalKeyValueStorage implements IKeyValueStorage {
    removeValue(key: string): void {
        window.localStorage.removeItem(key);
    }

    async getValue(key: string): Promise<string | null> {
        const encryptedItem: string | null = window.localStorage.getItem(key);

        if (encryptedItem === null) {
            return null;
        }

        return cipherComponent.decryptString(encryptedItem);
    }

    async setValue(key: string, value: string): Promise<void> {
        const encrypted: string | null = await cipherComponent.encryptString(value);

        if (encrypted === null) {
            console.error('Failed to encrypt value. (nothing stored)');
            return;
        }

        window.localStorage.setItem(key, encrypted);
    }
}

async function reloadVault(): Promise<void> {
    if (await ensureVaultStorage() === false) {
        return;
    }

    if (vaultStorage === null) {
        return;
    }

    txtVault.value = (await vaultStorage.getVaultContent()) || '<error>';
}

async function onRefreshVaultButtonClick(): Promise<void> {
    await reloadVault();
}

function onClearVaultSettingsButtonClick(): void {
    if (prompt('Are you sure you want to clear the vault settings ?\nType \'y\' to accept', '') !== 'y') {
        return;
    }

    window.localStorage.removeItem(LOCAL_STORAGE_KEY_USERNAME);
    window.localStorage.removeItem(LOCAL_STORAGE_KEY_PASSWORD_PUBLIC);
    window.localStorage.removeItem(LOCAL_STORAGE_KEY_PASSWORD_LENGTH);
    window.localStorage.removeItem(LOCAL_STORAGE_KEY_REPO);
    window.localStorage.removeItem(LOCAL_STORAGE_KEY_FILENAME);
}

function getSetVaultParameter(key: string, promptText: string): string | null {
    let value: string | null = window.localStorage.getItem(key);

    if (value) {
        return value;
    }

    value = prompt(promptText);

    if (!value) {
        return null;
    }

    window.localStorage.setItem(key, value);

    return value;
}

async function ensureVaultStorage(): Promise<boolean> {
    const username: string | null = getSetVaultParameter(LOCAL_STORAGE_KEY_USERNAME, 'GitHub account username:');
    if (!username) {
        return false;
    }

    const passwordPublicPart: string | null = getSetVaultParameter(LOCAL_STORAGE_KEY_PASSWORD_PUBLIC, 'GitHub account password public part:');
    if (!passwordPublicPart) {
        return false;
    }

    const passwordLengthString: string | null = getSetVaultParameter(LOCAL_STORAGE_KEY_PASSWORD_LENGTH, 'GitHub account password length:');
    if (!passwordLengthString) {
        return false;
    }

    const passwordLength = parseInt(passwordLengthString, 10);
    if (Number.isSafeInteger(passwordLength) === false || passwordLength <= 0) {
        return false;
    }

    const repositoryName: string | null = getSetVaultParameter(LOCAL_STORAGE_KEY_REPO, 'Vault GitHub repository name:');
    if (!repositoryName) {
        return false;
    }

    const vaultFilename: string | null = getSetVaultParameter(LOCAL_STORAGE_KEY_FILENAME, 'Vault filename:');
    if (!vaultFilename) {
        return false;
    }

    const keyString: string | null = await passwordComponent.generatePasswordString(passwordPublicPart);
    if (!keyString) {
        return false;
    }

    vaultStorage = new GitHubVaultStorage(username, keyString.substr(0, passwordLength), repositoryName, vaultFilename, new SecureLocalKeyValueStorage());

    return true;
}

export class VaultComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabVault;
    }
    getTabContent(): HTMLInputElement {
        return divTabVault;
    }
    onTabSelected(): void {
        storageOutputComponent.hide();
    }

    init(): void {
        btnRefreshVault.addEventListener('click', onRefreshVaultButtonClick);
        btnClearVaultSettings.addEventListener('click', onClearVaultSettingsButtonClick);
    }
}
