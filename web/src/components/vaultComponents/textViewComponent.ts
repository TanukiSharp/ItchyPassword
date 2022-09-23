import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import * as plainObject from '../../PlainObject';

const btnTabVaultTabTextView = getElementById('btnTabVaultTabTextView') as HTMLButtonElement;
const divTabVaultTabTextView = getElementById('divTabVaultTabTextView');

const txtVault = getElementById('txtVault') as HTMLInputElement;

export class VaultTextViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public readonly name: string = 'VaultTextView';

    public onVaultLoaded(vault: plainObject.PlainObject): void {
        txtVault.value = JSON.stringify(vault, undefined, 4);
    }

    public getTabButton(): HTMLButtonElement {
        return btnTabVaultTabTextView;
    }

    public getTabContent(): HTMLElement {
        return divTabVaultTabTextView;
    }

    public onTabSelected(): void {
    }

    public getVaultHint(): string {
        throw new Error('Not supported.');
    }

    public init(): void {
    }
}
