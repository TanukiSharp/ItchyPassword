import { getElementById, setupFeedbackButton } from '../ui';

import { IComponent } from './IComponent';
import { ITabInfo } from '../TabControl';

import * as storageOutputComponent from './storageOutputComponent';

import { SecureLocalStorage } from '../storages/SecureLocalStorage';
import { IVaultStorage } from '../storages/IVaultStorage';
import { GitHubPersonalAccessTokenVaultStorage } from '../storages/GitHubVaultStorage';
import { hasPrivatePart } from './privatePartComponent';
import * as plainObject from '../PlainObject';

const divTabVault: HTMLInputElement = getElementById('divTabVault');
const btnTabVault: HTMLInputElement = getElementById('btnTabVault');

const txtVault: HTMLInputElement = getElementById('txtVault');
const btnRefreshVault: HTMLInputElement = getElementById('btnRefreshVault');
const btnClearVaultSettings: HTMLInputElement = getElementById('btnClearVaultSettings');
const treeVault: HTMLElement = getElementById('treeVault');
const txtSearchVault: HTMLInputElement = getElementById('txtSearchVault');

let vaultStorage: IVaultStorage = new GitHubPersonalAccessTokenVaultStorage(new SecureLocalStorage());

const DEEP_MODE_NONE = 0;
const DEEP_MODE_UP = 1;
const DEEP_MODE_DOWN = 2;

const TREE_ELEMENT_HEIGHT = 24;

const HORIZONTAL_LINE_VERTICAL_OFFSET = 11;
const HORIZONTAL_LINE_LENGTH = 12;
const VERTICAL_BAR_OFFSET = 6;

interface PositionMarker {
    pos: number;
    len: number;
}

class TreeNode {
    protected readonly parent: TreeNode | null;
    protected readonly children: TreeNode[] = [];

    protected readonly rootElement: HTMLElement;
    protected readonly titleElement: HTMLElement;
    protected readonly childrenContainerElement: HTMLElement;

    protected readonly _propertyName: string;
    protected readonly propertyValue: any;

    protected readonly isHint: boolean = false;
    protected readonly isCipher: boolean = false;
    protected readonly isPassword: boolean = false;

    public get element(): HTMLElement {
        return this.rootElement;
    }

    public get propertyName(): string {
        return this._propertyName;
    }

    public get isVisible(): boolean {
        return this.rootElement.style.display !== 'none';
    }

    public getVisibleChildCount(): number {
        let visibleChildCount = 0;

        for (const child of this.children) {
            if (child.isVisible) {
                visibleChildCount += 1;
            }
        }

        return visibleChildCount;
    }

    public getVisibleLeafCount(): number {
        if (this.isVisible === false) {
            return 0;
        }

        let visibleLeafCount = 1;

        for (const child of this.children) {
            visibleLeafCount += child.getVisibleLeafCount();
        }

        return visibleLeafCount;
    }

    private addChild(child: TreeNode) {
        this.childrenContainerElement.appendChild(child.rootElement);
        this.children.push(child);
    }

    constructor(parent: TreeNode | null, path: string, title: string, value: any) {
        this.parent = parent;

        this.rootElement = document.createElement('div');
        this.setRootElementStyle();

        // Construct title DOM element.
        this.titleElement = document.createElement('div');
        this.titleElement.innerText = title;
        this.rootElement.appendChild(this.titleElement);
        this.setTitleElementStyle();

        // Construct children container DOM element.
        this.childrenContainerElement = document.createElement('div');
        this.rootElement.appendChild(this.childrenContainerElement);
        this.setChildrenContainerElementStyle();

        let isLeaf: boolean = false;

        if (isCiphersObject(title, value)) {
            //this.titleElement.style.backgroundColor = '#FF0000';
        } else if (isCipherObject(value)) {
            //this.titleElement.style.backgroundColor = '#FF8080';
            this.isCipher = true;
            isLeaf = true;
        } else if (isPasswordObject(title, value)) {
            //this.titleElement.style.backgroundColor = '#0000FF';
            this.isPassword = true;
            isLeaf = true;
        } else if (plainObject.isPlainObject(value)) {
            //this.titleElement.style.backgroundColor = '#00FF00';
        } else {
            //this.titleElement.style.backgroundColor = '#FFFF00';
            this.isHint = true;
        }

        this._propertyName = title;

        if (isLeaf === false && plainObject.isPlainObject(value)) {
            for (const [childKey, childValue] of Object.entries(value)) {
                const child = new TreeNode(this, `${path}/${childKey}`, childKey, childValue);
                this.addChild(child);
            }
        } else {
            this.propertyValue = value;

            // const button = document.createElement('button');
            // button.innerText = title;
            // this.titleElement.innerHTML = '';
            // this.titleElement.appendChild(button);
        }

        if (parent) {
            // Construct lines DOM elements.
            this.setupLinesElements('#D0D0D0');
        }
    }

    private setRootElementStyle(): void {
        this.rootElement.style.display = 'grid';
        this.rootElement.style.gridTemplateRows = `${TREE_ELEMENT_HEIGHT}px 1fr`;

        let childrenOffset = 0;
        if (this.parent && this.parent.parent) {
            childrenOffset = HORIZONTAL_LINE_LENGTH;
        }

        this.rootElement.style.gridTemplateColumns = `${childrenOffset}px ${VERTICAL_BAR_OFFSET}px 1fr`;
    }

    private verticalLineElement: HTMLElement | null = null;

    private setupLinesElements(color: string): void {
        const verticalLineElement = document.createElement('div');
        verticalLineElement.style.gridColumn = '2';
        verticalLineElement.style.gridRow = '2';
        verticalLineElement.style.width = '100%';
        verticalLineElement.style.borderRight = `1px solid ${color}`;
        this.verticalLineElement = verticalLineElement;
        this.rootElement.appendChild(verticalLineElement);

        if (this.parent && this.parent.parent) {
            const horizontalLineElement = document.createElement('div');
            horizontalLineElement.style.gridColumn = '1';
            horizontalLineElement.style.gridRow = '1';
            horizontalLineElement.style.width = '100%';
            horizontalLineElement.style.height = `${HORIZONTAL_LINE_VERTICAL_OFFSET}px`;
            horizontalLineElement.style.borderBottom = `1px solid ${color}`;
            this.rootElement.appendChild(horizontalLineElement);
        }

        this.updateLines();
    }

    private updateLines(): void {
        if (this.verticalLineElement === null) {
            return;
        }

        const visibleChildCount = this.getVisibleChildCount();

        if (visibleChildCount === 0) {
            this.verticalLineElement.style.height = '0px';
            return;
        }

        let totalVisibleLeafCount = 1;

        for (let i = 0; i < visibleChildCount - 1; i += 1) {
            if (this.children[i].isVisible) {
                totalVisibleLeafCount += this.children[i].getVisibleLeafCount();
            }
        }

        const bottomPosition = (totalVisibleLeafCount * TREE_ELEMENT_HEIGHT) - TREE_ELEMENT_HEIGHT + HORIZONTAL_LINE_VERTICAL_OFFSET + 1;

        this.verticalLineElement.style.height = `${bottomPosition}px`;
    }

    private setTitleElementStyle(): void {
        this.titleElement.style.gridColumn = '2 / span 2';
        this.titleElement.style.gridRow = '1';
        this.titleElement.style.marginLeft = '3px';
    }

    private setChildrenContainerElementStyle(): void {
        this.childrenContainerElement.style.gridColumn = '3';
        this.childrenContainerElement.style.gridRow = '2';
    }

    private resetTitle(deepMode: number): void {
        this.titleElement.innerHTML = '';
        this.titleElement.innerText = this.propertyName;

        if (deepMode === DEEP_MODE_UP && this.parent) {
            this.parent.resetTitle(deepMode);
        }

        if (deepMode === DEEP_MODE_DOWN) {
            for (const child of this.children) {
                child.resetTitle(deepMode);
            }
        }
    }

    private show(deepMode: number): void {
        this.rootElement.style.display = 'grid';

        if (deepMode === DEEP_MODE_UP && this.parent) {
            this.parent.show(deepMode);
        }

        if (deepMode === DEEP_MODE_DOWN) {
            for (const child of this.children) {
                child.show(deepMode);
            }
        }

        this.updateLines();
    }

    private hide(deepMode: number): void {
        this.rootElement.style.display = 'none';

        if (deepMode === DEEP_MODE_UP && this.parent) {
            this.parent.hide(deepMode);
        }

        if (deepMode === DEEP_MODE_DOWN) {
            for (const child of this.children) {
                child.hide(deepMode);
            }
        }

        this.updateLines();
    }

    private static createSpan(text: string, color?: string): HTMLElement {
        const element = document.createElement('span');
        if (color) {
            element.style.backgroundColor = color;
            element.style.borderRadius = '2px';
        }
        element.innerText = text;
        return element;
    }

    private static createColoredSpan(text: string, markers: PositionMarker[]): HTMLElement {
        const root = document.createElement('span');

        let pos = 0;

        for (const marker of markers) {
            if (marker.pos !== pos) {
                root.appendChild(TreeNode.createSpan(text.substr(pos, marker.pos - pos)));
            }

            root.appendChild(TreeNode.createSpan(text.substr(marker.pos, marker.len), '#80C0FF'));

            pos = marker.pos + marker.len;
        }

        if (pos < text.length) {
            root.appendChild(TreeNode.createSpan(text.substr(pos, text.length - pos)));
        }

        return root;
    }

    private static indexedMatchFunction(lhs: string, lhsIndex: number, rhs: string, markers: PositionMarker[]): boolean {
        if (!rhs) {
            return true;
        }

        for (let len = rhs.length; len >= 1; len -= 1) {
            const subWord = rhs.substr(0, len);
            const foundPos = lhs.indexOf(subWord, lhsIndex);

            if (foundPos >= 0) {
                markers.push({
                    pos: foundPos,
                    len: subWord.length
                });

                return TreeNode.indexedMatchFunction(lhs, foundPos + subWord.length, rhs.substr(len), markers);
            }
        }

        return false;
    }

    private static matchFunction(lhs: string, rhs: string, markers: PositionMarker[]): boolean {
        return TreeNode.indexedMatchFunction(lhs.toLocaleLowerCase(), 0, rhs.toLocaleLowerCase(), markers);
    }

    public filter(searchText: string): void {
        if (!searchText) {
            this.show(DEEP_MODE_DOWN);
            this.resetTitle(DEEP_MODE_DOWN);
            this.updateLines();
            return;
        }

        this.resetTitle(DEEP_MODE_DOWN);

        const markers: PositionMarker[] = [];
        const isVisible = TreeNode.matchFunction(this.propertyName, searchText, markers);

        this.titleElement.innerHTML = '';
        this.titleElement.appendChild(TreeNode.createColoredSpan(this.propertyName, markers));

        if (isVisible) {
            this.show(DEEP_MODE_UP);
            this.show(DEEP_MODE_DOWN);
        } else {
            this.hide(DEEP_MODE_NONE);
            this.resetTitle(DEEP_MODE_NONE);

            for (const child of this.children) {
                child.filter(searchText);
            }
        }

        this.updateLines();
    }
}


function isPasswordObject(key: string, obj: plainObject.PlainObject): boolean {
    if (key !== 'password') {
        return false;
    }

    if (!obj || !plainObject.isPlainObject(obj) || typeof obj.public !== 'string' || obj.public.length < 4) {
        return false;
    }

    return true;
}

function isCipherObject(obj: plainObject.PlainObject): boolean {
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

function isCiphersObject(key: string, obj: plainObject.PlainObject): boolean {
    if (key !== 'ciphers') {
        return false;
    }

    if (!obj || !plainObject.isPlainObject(obj)) {
        return false;
    }

    for (const sub of Object.values(obj)) {
        if (!isCipherObject(sub)) {
            return false;
        }
    }

    return true;
}

function isItchyObject(obj: plainObject.PlainObject): boolean {
    for (const [key, value] of Object.entries(obj)) {
        if (!isCiphersObject(key, value)) {
            return false;
        }
        if (!isPasswordObject(key, value)) {
            return false;
        }
    }

    return true;
}

let rootTreeNode: TreeNode;

async function reloadVault(): Promise<boolean> {
    let content: string | null = await vaultStorage.getVaultContent();

    // const testObject = {
    //     key1: {
    //         anotherSubObject: 'test',
    //         subObject: {
    //             longVarName: 51,
    //             evenLongerVarName: 51
    //         },
    //         zzz: {
    //             sleep: true,
    //             lol: false
    //         }
    //     },
    //     key2: {
    //         a: 1,
    //         b: 2,
    //         c: {
    //             alice: 'bob',
    //             bob: 'charlie',
    //             charlie: 'alice'
    //         }
    //     }
    // };
    // let content: string | null = JSON.stringify(testObject);

    treeVault.innerHTML = '';

    if (content !== null) {
        try {
            let obj = JSON.parse(content) as plainObject.PlainObject;
            obj = plainObject.objectDeepSort(obj);

            rootTreeNode = new TreeNode(null, '<root>', '', obj);
            treeVault.appendChild(rootTreeNode.element);

            // Hack to prevent tree view from shrinking, set to maximum possible height after building the whole tree.
            setTimeout(() => treeVault.style.minHeight = `${treeVault.clientHeight}px`, 1);

            content = JSON.stringify(obj, undefined, 4);
        } catch (error) {
            console.error(error);
            content = null;
        }
    }

    txtVault.value = content || '<error>';

    return content !== null;
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

function onSearchVaultInputChanged(): void {
    if (rootTreeNode) {
        rootTreeNode.filter(txtSearchVault.value.toLocaleLowerCase());
    }
}

export class VaultComponent implements IComponent, ITabInfo {
    getTabButton(): HTMLInputElement {
        return btnTabVault;
    }
    getTabContent(): HTMLInputElement {
        return divTabVault;
    }
    onTabSelected(): void {
        storageOutputComponent.hide();
    }

    init(): void {
        setupFeedbackButton(btnRefreshVault, onRefreshVaultButtonClick);
        btnClearVaultSettings.addEventListener('click', onClearVaultSettingsButtonClick);
        txtSearchVault.addEventListener('input', onSearchVaultInputChanged)
    }
}
