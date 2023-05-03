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

    public constructor() {
        this.passwordService = serviceManager.getService('password');
        this.cipherService = serviceManager.getService('cipher');
    }

    private async runPassword(value: any): Promise<void> {
        await this.passwordService.generateAndCopyPasswordToClipboard(
            value.public,
            value.alphabet,
            value.length,
            value.version,
        );
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

    createTreeNodeContentElement(path: string, key: string, value: any): HTMLElement {
        if (VaultTreeNodeCreationController.isPasswordObject(key, value)) {
            const version: number = value.version;
            const isLatest = this.passwordService.isLatestVersion(version);

            const button = document.createElement('button');
            button.style.justifySelf = 'start';
            button.style.minWidth = '80px';
            button.innerText = 'Password';

            if (isLatest === false) {
                button.setAttribute('not-latest', `⚠️ Password version ${version}, latest is ${this.passwordService.getLatestVersion()}`);
                button.classList.add('not-latest');
            }

            const errorLogsService = serviceManager.getService('errorLogs');
            const logFunc = errorLogsService.createLogErrorMessageFunction();

            ui.setupFeedbackButton(button, async () => await this.runPassword(value), logFunc);

            return button;
        } else if (VaultTreeNodeCreationController.isCipherObject(value)) {
            const version: number = value.version;
            const isLatest = this.cipherService.isLatestVersion(version);

            const button = document.createElement('button');
            button.style.justifySelf = 'start';
            button.innerText = key;
            button.title = 'Open in ciphers';

            if (isLatest === false) {
                button.setAttribute('not-latest', `⚠️ Cipher version ${version}, latest is ${this.cipherService.getLatestVersion()}`);
                button.classList.add('not-latest');
            }

            const errorLogsService = serviceManager.getService('errorLogs');
            const logFunc = errorLogsService.createLogErrorMessageFunction();

            ui.setupFeedbackButton(button, async () => await this.runCipher(path, key, value), logFunc);

            return button;
        } else if (VaultTreeNodeCreationController.isHint(key, value)) {
            const label = document.createElement('span');
            label.style.justifySelf = 'start';
            label.innerText = `${key}: ${value}`;

            return label;
        }

        const div = document.createElement('div');
        div.innerText = key;
        return div;
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public readonly name: string = 'VaultTreeView';

    public onVaultLoaded(vault: plainObject.PlainObject): void {
        rootTreeNode = new TreeNode(null, '<root>', '', vault, new VaultTreeNodeCreationController());

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
