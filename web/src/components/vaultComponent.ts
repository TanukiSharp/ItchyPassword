import { getElementById, setupFeedbackButton } from '../ui';

import { IComponent } from './IComponent';
import { ITabInfo, TabControl } from '../TabControl';

import * as storageOutputComponent from './storageOutputComponent';

import { SecureLocalStorage } from '../storages/SecureLocalStorage';
import { IVaultStorage } from '../storages/IVaultStorage';
import { GitHubPersonalAccessTokenVaultStorage } from '../storages/GitHubVaultStorage';
import { hasPrivatePart } from './privatePartComponent';
import * as plainObject from '../PlainObject';
import { VaultTreeViewComponent } from './vaultComponents/treeViewComponent';
import { VaultTextViewComponent } from './vaultComponents/textViewComponent';

export interface IVaultComponent {
    onVaultLoaded(vault: plainObject.PlainObject): void;
}

const divTabVault: HTMLInputElement = getElementById('divTabVault');
const btnTabVault: HTMLInputElement = getElementById('btnTabVault');

const btnRefreshVault: HTMLInputElement = getElementById('btnRefreshVault');
const btnClearVaultSettings: HTMLInputElement = getElementById('btnClearVaultSettings');

const elements: any[] = [
    new VaultTreeViewComponent(),
    new VaultTextViewComponent(),
];

const tabs: ITabInfo[] = elements.filter(e => (e as ITabInfo).getTabButton !== undefined);
const components: (IComponent & IVaultComponent)[] = elements.filter(e => (e as IComponent).init !== undefined);

new TabControl(tabs);

let vaultStorage: IVaultStorage = new GitHubPersonalAccessTokenVaultStorage(new SecureLocalStorage());

async function reloadVault(): Promise<boolean> {
    let content: string | null = await vaultStorage.getVaultContent();

    if (content === null) {
        return false;
    }

    try {
        let obj = JSON.parse(content) as plainObject.PlainObject;
        obj = plainObject.objectDeepSort(obj);

        let component: IVaultComponent;
        for (component of components) {
            component.onVaultLoaded(obj);
        }

        return true;
    } catch (error) {
        console.error(error);
        return false;
    }
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

        let component: IComponent;
        for (component of components) {
            component.init();
        }
    }
}
