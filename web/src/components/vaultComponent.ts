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

import * as serviceManager from '../services/serviceManger';
import { VaultService } from '../services/vaultService';

export interface IVaultComponent {
    onVaultLoaded(vault: plainObject.PlainObject): void;
}

const divTabVault = getElementById('divTabVault');
const btnTabVault = getElementById('btnTabVault') as HTMLButtonElement;

const btnRefreshVault = getElementById('btnRefreshVault') as HTMLButtonElement;
const btnClearVaultSettings = getElementById('btnClearVaultSettings') as HTMLButtonElement;

const elements: any[] = [
    new VaultTreeViewComponent(),
    new VaultTextViewComponent(),
];

const tabs: ITabInfo[] = elements.filter(e => (e as ITabInfo).getTabButton !== undefined);
const components: (IComponent & IVaultComponent)[] = elements.filter(e => (e as IComponent).init !== undefined);

const subTabs = new TabControl(tabs);

let vaultStorage: IVaultStorage = new GitHubPersonalAccessTokenVaultStorage(new SecureLocalStorage());

let vaultObject: plainObject.PlainObject | null = null;

function computeUserPathMatchDepth(path: string): number {
    if (vaultObject === null) {
        return 0;
    }

    let obj = vaultObject;

    const pathArray = path.split('/');

    for (let i = 0; i < pathArray.length; i += 1) {
        if (!obj[pathArray[i]]) {
            return i;
        }

        // TODO: Filter here to not go further down ItchyObjects.

        obj = obj[pathArray[i]];
    }

    return pathArray.length;
}

async function reloadVault(): Promise<boolean> {
    let content: string | null = await vaultStorage.getVaultContent();

    if (content === null) {
        return false;
    }

    try {
        let obj = JSON.parse(content) as plainObject.PlainObject;
        obj = plainObject.objectDeepSort(obj);

        vaultObject = obj;

        let component: IVaultComponent;
        for (component of components) {
            component.onVaultLoaded(obj);
        }

        return true;
    } catch (error) {
        vaultObject = null;
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
    public readonly name: string = 'Vault';

    public computeUserPathMatchDepth(path: string) {
        return computeUserPathMatchDepth(path);
    }

    public getTabButton(): HTMLButtonElement {
        return btnTabVault;
    }

    public getTabContent(): HTMLElement {
        return divTabVault;
    }

    public onTabSelected(): void {
        storageOutputComponent.hide();
        tabs[subTabs.activeTabIndex].onTabSelected();
    }

    public init(): void {
        setupFeedbackButton(btnRefreshVault, onRefreshVaultButtonClick);
        btnClearVaultSettings.addEventListener('click', onClearVaultSettingsButtonClick);

        const vaultService = new VaultService(this);
        serviceManager.registerService('vault', vaultService);

        let component: IComponent;
        for (component of components) {
            component.init();
        }
    }
}
