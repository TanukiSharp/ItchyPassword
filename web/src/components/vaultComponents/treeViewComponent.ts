import { getElementById } from '../../ui';
import { IComponent } from '../IComponent';
import { ITabInfo } from '../../TabControl';
import { IVaultComponent } from '../vaultComponent';
import { TreeNode } from './TreeNode';
import * as plainObject from '../../PlainObject';

const btnTabVaultTabTreeView = getElementById('btnTabVaultTabTreeView');
const divTabVaultTabTreeView = getElementById('divTabVaultTabTreeView');

const divVaultTreeViewSearchAndTree = getElementById('divVaultTreeViewSearchAndTree');

const treeVault: HTMLElement = getElementById('treeVault');
const txtSearchVault: HTMLInputElement = getElementById('txtSearchVault');

let rootTreeNode: TreeNode;

function onSearchVaultInputChanged(): void {
    if (rootTreeNode) {
        rootTreeNode.filter(txtSearchVault.value.toLocaleLowerCase());
    }
}

export class VaultTreeViewComponent implements IComponent, ITabInfo, IVaultComponent {
    onVaultLoaded(vault: plainObject.PlainObject): void {
        rootTreeNode = new TreeNode(null, '<root>', '', vault);

        treeVault.innerHTML = '';
        treeVault.appendChild(rootTreeNode.element);

        // Hack to prevent tree view from shrinking, set to maximum possible height after building the whole tree.
        setTimeout(() => treeVault.style.minHeight = `${treeVault.clientHeight}px`, 1);

        divVaultTreeViewSearchAndTree.style.removeProperty('display');
    }

    public getTabButton(): HTMLInputElement {
        return btnTabVaultTabTreeView;
    }
    public getTabContent(): HTMLInputElement {
        return divTabVaultTabTreeView;
    }

    public onTabSelected(): void {
    }

    public init(): void {
        divVaultTreeViewSearchAndTree.style.display = 'none';
        txtSearchVault.addEventListener('input', onSearchVaultInputChanged)
    }
}
