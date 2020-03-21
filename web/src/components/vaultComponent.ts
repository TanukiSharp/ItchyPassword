import { getElementById, setupFeedbackButton } from '../ui';

import { IComponent } from './IComponent';
import { ITabInfo } from '../TabControl';

import * as storageOutputComponent from './storageOutputComponent';

import { SecureLocalStorage } from '../storages/SecureLocalStorage';
import { IVaultStorage } from '../storages/IVaultStorage';
import { GitHubPersonalAccessTokenVaultStorage } from '../storages/GitHubVaultStorage';
import { hasPrivatePart } from './privatePartComponent';

const divTabVault: HTMLInputElement = getElementById('divTabVault');
const btnTabVault: HTMLInputElement = getElementById('btnTabVault');

const txtVault: HTMLInputElement = getElementById('txtVault');
const btnRefreshVault: HTMLInputElement = getElementById('btnRefreshVault');
const btnClearVaultSettings: HTMLInputElement = getElementById('btnClearVaultSettings');

let vaultStorage: IVaultStorage = new GitHubPersonalAccessTokenVaultStorage(new SecureLocalStorage());

async function reloadVault(): Promise<boolean> {
    const content: string | null = await vaultStorage.getVaultContent();
    txtVault.value = content || '<error>';
    return content !== null;
}

async function onRefreshVaultButtonClick(): Promise<boolean> {
    if (hasPrivatePart() === false) {
        alert('You must enter a master key first.');
        return false;
    }

    return await reloadVault();
}

function onClearVaultSettingsButtonClick(): void {
    if (prompt('Are you sure you want to clear the vault settings ?\nType \'y\' to accept', '') !== 'y') {
        return;
    }

    vaultStorage.clear();
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
        setupFeedbackButton(btnRefreshVault, onRefreshVaultButtonClick);
        btnClearVaultSettings.addEventListener('click', onClearVaultSettingsButtonClick);
    }
}