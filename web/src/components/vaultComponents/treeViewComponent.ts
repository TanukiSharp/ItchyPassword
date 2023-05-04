import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import { TreeNode, TreeNodeCreationController, DEEP_MODE_DOWN } from './TreeNode';
import * as plainObject from '../../PlainObject';
import * as ui from '../../ui';
import { aggresiveSearchMatchFunction, containsSearchMatchFunction, SearchMatchFunction } from '../../searchMatchFunctions';
import * as serviceManager from '../../services/serviceManger';
import { PasswordService } from '../../services/passwordService';
import { CipherService } from 'services/cipherService';

const ONE_YEAR_IN_MILLISECONDS = 365 * 24 * 3600 * 1000;

const btnTabVaultTabTreeView = getElementById('btnTabVaultTabTreeView') as HTMLButtonElement;
const divTabVaultTabTreeView = getElementById('divTabVaultTabTreeView');

const trvVaultTreeView = getElementById('trvVaultTreeView');
const txtVaultTreeViewSearch = getElementById('txtVaultTreeViewSearch') as HTMLInputElement;
const cboVaultTreeViewSearchType = getElementById('cboVaultTreeViewSearchType') as HTMLSelectElement;

let rootTreeNode: TreeNode;

interface SearchMatchFunctionDescription {
    text: string,
    function: SearchMatchFunction
}

const searchMatchFunctionDescriptions: SearchMatchFunctionDescription[] = [
    { text: 'Aggresive', function: aggresiveSearchMatchFunction },
    { text: 'Regular', function: containsSearchMatchFunction },
];

function onSearchVaultInputChanged(): void {
    if (!rootTreeNode) {
        return;
    }

    const index: number = cboVaultTreeViewSearchType.selectedIndex;
    const searchMatchFunction: SearchMatchFunction = searchMatchFunctionDescriptions[index].function;

    rootTreeNode.hide(DEEP_MODE_DOWN);
    rootTreeNode.filter(txtVaultTreeViewSearch.value.toLocaleLowerCase(), searchMatchFunction);
}

function populateSearchFunctions(): void {
    cboVaultTreeViewSearchType.innerHTML = '';

    for (let description of searchMatchFunctionDescriptions) {
        const option = document.createElement('option');
        option.text = description.text;
        cboVaultTreeViewSearchType.appendChild(option);
    }
}

class VaultTreeNodeCreationController implements TreeNodeCreationController {
    private readonly passwordService: PasswordService;
    private readonly cipherService: CipherService;
    private readonly buttonBackgroundColor: string;

    public constructor(private readonly maxTimestampInMilliseconds: number) {
        this.passwordService = serviceManager.getService('password');
        this.cipherService = serviceManager.getService('cipher');

        const buttonStyle = window.getComputedStyle(btnTabVaultTabTreeView);

        this.buttonBackgroundColor = buttonStyle.backgroundColor;
    }

    private async runPassword(value: any): Promise<void> {
        await this.passwordService.generateAndCopyPasswordToClipboard(
            value.public,
            value.alphabet,
            value.length,
            value.version,
        );
    }

    private editPassword(path: string, value: any): boolean {
        return this.passwordService.activate(path, value);
    }

    private async runCipher(path: string, key: string, value: any): Promise<boolean> {
        return await this.cipherService.activate(path, key, value);
    }

    private static isPasswordObject(key: string, obj: plainObject.PlainObject): boolean {
        if (key !== 'password') {
            return false;
        }

        if (!obj || !plainObject.isPlainObject(obj) || typeof obj.public !== 'string' || obj.public.length < 4) {
            return false;
        }

        return true;
    }

    private static isCipherObject(obj: plainObject.PlainObject): boolean {
        if (!obj || !plainObject.isPlainObject(obj)) {
            return false;
        }

        if (typeof obj.value !== 'string' || obj.value.length <= 0) {
            return false;
        }

        if (typeof obj.version !== 'number' || obj.version < 0) {
            return false;
        }

        return true;
    }

    private static isCiphersObject(key: string, obj: plainObject.PlainObject): boolean {
        if (key !== 'ciphers') {
            return false;
        }

        if (!obj || !plainObject.isPlainObject(obj)) {
            return false;
        }

        for (const sub of Object.values(obj)) {
            if (!VaultTreeNodeCreationController.isCipherObject(sub)) {
                return false;
            }
        }

        return true;
    }

    private static isHint(key: string, value: any) {
        if (VaultTreeNodeCreationController.isCiphersObject(key, value) ||
            VaultTreeNodeCreationController.isCipherObject(value) ||
            VaultTreeNodeCreationController.isPasswordObject(key, value) ||
            plainObject.isPlainObject(value)) {
            return false;
        }

        return true;
    }

    public isLeaf(path: string, key: string, value: any): boolean {
        if (VaultTreeNodeCreationController.isCipherObject(value) ||
            VaultTreeNodeCreationController.isPasswordObject(key, value)) {
            return true;
        }

        return plainObject.isPlainObject(value) === false;
    }

    private computeTimestampProgress(timestamp: Date): number {
        const now = Date.now()
        const then = timestamp.getTime();
        const diff = now - then;

        const timespan = Math.max(0, Math.min(diff, this.maxTimestampInMilliseconds));

        return timespan / this.maxTimestampInMilliseconds;
    }

    private static dateToString(date: Date): string {
        return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
    }

    private createButton(text: string, timestamp: Date) {
        const button = document.createElement('button');

        button.title = `Last modified: ${VaultTreeNodeCreationController.dateToString(timestamp)}`;

        const grid = document.createElement('div');
        grid.classList.add('content-container');

        const textSpan = document.createElement('span');
        textSpan.innerText = text;
        textSpan.classList.add('text');
        grid.appendChild(textSpan);

        const timestampDiv = document.createElement('div');
        timestampDiv.classList.add('timestamp');
        timestampDiv.style.backgroundColor = this.buttonBackgroundColor;
        timestampDiv.style.width = `${Math.round((1 - this.computeTimestampProgress(timestamp)) * 100)}%`;
        grid.appendChild(timestampDiv);

        button.appendChild(grid);

        return button;
    }

    public createTreeNodeContentElements(path: string, key: string, value: any): HTMLElement[] {
        if (VaultTreeNodeCreationController.isPasswordObject(key, value)) {
            const version: number = value.version;
            const isLatest = this.passwordService.isLatestVersion(version);

            const lastModified = new Date(value.datetime);

            const button = this.createButton('Password', lastModified);
            button.classList.add('password');
            button.style.justifySelf = 'start';
            button.style.minWidth = '80px';

            if (isLatest === false) {
                button.setAttribute('not-latest', `⚠️ Password version ${version}, latest is ${this.passwordService.getLatestVersion()}`);
                button.classList.add('not-latest');
            }

            const errorLogsService = serviceManager.getService('errorLogs');
            const logFunc = errorLogsService.createLogErrorMessageFunction();

            ui.setupFeedbackButton(button, async () => await this.runPassword(value), logFunc);

            const editButton = document.createElement('button');
            editButton.classList.add('edit-password');
            editButton.innerText = '✏️';
            editButton.title = 'Edit password generation details';

            ui.setupFeedbackButton(editButton, () => this.editPassword(path, value), logFunc);

            return [button, editButton];
        } else if (VaultTreeNodeCreationController.isCipherObject(value)) {
            const version: number = value.version;
            const isLatest = this.cipherService.isLatestVersion(version);

            const lastModified = new Date(value.datetime);

            const button = this.createButton(key, lastModified);
            button.classList.add('cipher');
            button.style.justifySelf = 'start';

            if (isLatest === false) {
                button.setAttribute('not-latest', `⚠️ Cipher version ${version}, latest is ${this.cipherService.getLatestVersion()}`);
                button.classList.add('not-latest');
            }

            const errorLogsService = serviceManager.getService('errorLogs');
            const logFunc = errorLogsService.createLogErrorMessageFunction();

            ui.setupFeedbackButton(button, async () => await this.runCipher(path, key, value), logFunc);

            return [button];
        } else if (VaultTreeNodeCreationController.isHint(key, value)) {
            const label = document.createElement('span');
            label.style.justifySelf = 'start';
            label.innerText = `${key}: ${value}`;

            return [label];
        }

        const div = document.createElement('div');
        div.innerText = key;
        return [div];
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public readonly name: string = 'VaultTreeView';

    public onVaultLoaded(vault: plainObject.PlainObject): void {
        const maxTimespan = 3 * ONE_YEAR_IN_MILLISECONDS;

        rootTreeNode = new TreeNode(null, '<root>', '', vault, new VaultTreeNodeCreationController(maxTimespan));

        trvVaultTreeView.innerHTML = '';
        trvVaultTreeView.appendChild(rootTreeNode.element);

        onSearchVaultInputChanged();
    }

    public getTabButton(): HTMLButtonElement {
        return btnTabVaultTabTreeView;
    }

    public getTabContent(): HTMLElement {
        return divTabVaultTabTreeView;
    }

    public onTabSelected(): void {
        txtVaultTreeViewSearch.focus();
    }

    public getVaultHint(): string {
        throw new Error('Not supported.');
    }

    public init(): void {
        populateSearchFunctions();

        txtVaultTreeViewSearch.addEventListener('input', onSearchVaultInputChanged);
        cboVaultTreeViewSearchType.addEventListener('change', onSearchVaultInputChanged);
    }
}
