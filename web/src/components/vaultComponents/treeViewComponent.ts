import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import { TreeNode, TreeNodeTitleElementFactory, TreeNodeContext } from './TreeNode';
import * as plainObject from '../../PlainObject';
import { aggresiveSearchMatchFunction, containsSearchMatchFunction, SearchMatchFunction } from '../../searchMatchFunctions';

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
    createTreeNodeTitleElement(context: TreeNodeContext): HTMLElement {
        return document.createElement('div');
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    onVaultLoaded(vault: plainObject.PlainObject): void {
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
    }

    public init(): void {
        populateSearchFunctions();

        txtVaultTreeViewSearch.addEventListener('input', onSearchVaultInputChanged);
        cboVaultTreeViewSearchType.addEventListener('change', onSearchVaultInputChanged);
    }
}
