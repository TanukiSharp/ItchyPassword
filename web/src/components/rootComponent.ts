import { getElementById } from '../ui';
import { TabControl, ITabInfo } from '../TabControl';

import { IComponent } from './IComponent';
import { PrivatePartComponent } from './privatePartComponent';
import { PasswordComponent } from './passwordComponent';
import { CipherComponent } from './cipherComponent';
import { ReEncryptComponent } from './reEncryptComponent';
import { VaultComponent } from './vaultComponent';

import * as storageOutputComponent from './storageOutputComponent';

const nothingTabInfo: ITabInfo = {
    getTabButton(): HTMLButtonElement {
        return getElementById('btnTabNothing') as HTMLButtonElement;
    },
    getTabContent(): HTMLElement {
        return getElementById('divTabNothing');
    },
    onTabSelected(): void {
        storageOutputComponent.hide();
    }
}

const elements: any[] = [
    nothingTabInfo,
    new PrivatePartComponent(),
    new PasswordComponent(),
    new CipherComponent(),
    new ReEncryptComponent(),
    new storageOutputComponent.StorageOutputComponent(),
    new VaultComponent(),
];

const tabs: ITabInfo[] = elements.filter(e => (e as ITabInfo).getTabButton !== undefined);
const components: IComponent[] = elements.filter(e => (e as IComponent).init !== undefined);

const tabControl = new TabControl(tabs);

export class RootComponent implements IComponent {
    public readonly name: string = 'Root';

    public constructor() {
    }

    public getVaultHint(): string {
        throw new Error('Not supported.');
    }

    public init(): void {
        let component: IComponent;
        for (component of components) {
            component.init();
        }
    }

    public getActiveComponent(): IComponent | null {
        const component = tabs[tabControl.activeTabIndex] as any;

        if (component.init !== undefined) {
            return component as IComponent;
        }

        return null;
    }
}

export const rootComponent = new RootComponent();
