import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import * as plainObject from '../../PlainObject';

const btnTabVaultTabTextView = getElementById('btnTabVaultTabTextView');
const divTabVaultTabTextView = getElementById('divTabVaultTabTextView');

const txtVault: HTMLInputElement = getElementById('txtVault');

export class VaultTextViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public onVaultLoaded(vault: plainObject.PlainObject): void {
        txtVault.value = JSON.stringify(vault, undefined, 4);
    }

    public getTabButton(): HTMLInputElement {
        return btnTabVaultTabTextView;
    }
    public getTabContent(): HTMLInputElement {
        return divTabVaultTabTextView;
    }

    public onTabSelected(): void {
    }

    public init(): void {
    }
}
