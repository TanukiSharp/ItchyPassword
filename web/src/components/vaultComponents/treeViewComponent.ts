import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import { TreeNode, TreeNodeTitleElementFactory, TreeNodeContext, DEEP_MODE_DOWN } from './TreeNode';
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

class VaultTreeNodeTitleElementFactory implements TreeNodeTitleElementFactory {
    private readonly passwordService: PasswordService;

    public constructor() {
        this.passwordService = serviceManager.getService('password');
    }

    private async run(context: TreeNodeContext): Promise<void> {
        const value = context.value;
        await this.passwordService.generateAndCopyPasswordToClipboard(value.public, value.alphabet, value.length);
    }

    createTreeNodeTitleElement(context: TreeNodeContext): HTMLElement {
        if (context.isPassword) {
            const button = document.createElement('button');
            button.style.justifySelf = 'start';
            button.style.minWidth = '80px';

            ui.setupFeedbackButton(button, async () => await this.run(context));

            return button;
        }

        return document.createElement('div');
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    public onVaultLoaded(vault: plainObject.PlainObject): void {
        rootTreeNode = new TreeNode(null, '<root>', '', new VaultTreeNodeTitleElementFactory(), vault);

        trvVaultTreeView.innerHTML = '';
        trvVaultTreeView.appendChild(rootTreeNode.element);
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
