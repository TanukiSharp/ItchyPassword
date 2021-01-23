import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import { TreeNode, TreeNodeContentElementFactory, TreeNodeContext, DEEP_MODE_DOWN } from './TreeNode';
import * as plainObject from '../../PlainObject';
import * as ui from '../../ui';
import { aggresiveSearchMatchFunction, containsSearchMatchFunction, SearchMatchFunction } from '../../searchMatchFunctions';
import * as serviceManager from '../../services/serviceManger';
import { PasswordService } from '../../services/passwordService';

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

class VaultTreeNodeContentElementFactory implements TreeNodeContentElementFactory {
    private readonly passwordService: PasswordService;

    public constructor() {
        this.passwordService = serviceManager.getService('password');
    }

    private async run(context: TreeNodeContext): Promise<void> {
        const value = context.value;
        await this.passwordService.generateAndCopyPasswordToClipboard(value.public, value.alphabet, value.length);
    }

    createTreeNodeContentElement(context: TreeNodeContext): HTMLElement {
        if (context.isPassword) {
            const button = document.createElement('button');
            button.style.justifySelf = 'start';
            button.style.minWidth = '80px';
            button.innerText = 'Password';

            ui.setupFeedbackButton(button, async () => await this.run(context));

            return button;
        } else if (context.isHint) {
            const label = document.createElement('span');
            label.style.justifySelf = 'start';
            label.innerText = `${context.key}: ${context.value}`;

            return label;
        }

        const div = document.createElement('div');
        div.innerText = context.key;
        return div;
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public readonly name: string = 'VaultTreeView';

    public onVaultLoaded(vault: plainObject.PlainObject): void {
        rootTreeNode = new TreeNode(null, '<root>', '', new VaultTreeNodeContentElementFactory(), vault);

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

    public init(): void {
        populateSearchFunctions();

        txtVaultTreeViewSearch.addEventListener('input', onSearchVaultInputChanged);
        cboVaultTreeViewSearchType.addEventListener('change', onSearchVaultInputChanged);
    }
}
