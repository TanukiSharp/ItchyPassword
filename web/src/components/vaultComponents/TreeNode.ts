import * as plainObject from '../../PlainObject';
import { SearchMatchFunction, PositionMarker } from '../../searchMatchFunctions';

export const DEEP_MODE_NONE = 0;
export const DEEP_MODE_UP = 1;
export const DEEP_MODE_DOWN = 2;

const TREE_ELEMENT_HEIGHT = 24;

const HORIZONTAL_LINE_VERTICAL_OFFSET = 11;
const HORIZONTAL_LINE_LENGTH = 12;
const VERTICAL_BAR_OFFSET = 6;

export interface TreeNodeCreationController {
    isLeaf(path: string, key: string, value: any): boolean;
    createTreeNodeContentElement(path: string, key: string, value: any): HTMLElement;
}

export class TreeNode {
    protected readonly parent: TreeNode | null;
    protected readonly children: TreeNode[] = [];

    protected readonly rootElement: HTMLElement;
    protected readonly titleElement: HTMLElement;
    protected readonly childrenContainerElement: HTMLElement;

    protected readonly treeNodeCreationController: TreeNodeCreationController;

    protected readonly path: string;
    protected readonly key: string;
    protected readonly value: any;

    public get element(): HTMLElement {
        return this.rootElement;
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

    constructor(parent: TreeNode | null, path: string, key: string, value: any, treeNodeCreationController: TreeNodeCreationController) {
        this.parent = parent;
        this.path = path;
        this.key = key;
        this.value = value;
        this.treeNodeCreationController = treeNodeCreationController;

        this.rootElement = document.createElement('div');
        this.setRootElementStyle();


        // Construct title DOM element.
        this.titleElement = document.createElement('div');
        this.setTitleElementStyle();

        this.titleElement.appendChild(this.createTreeNodeContentElement());

        this.rootElement.appendChild(this.titleElement);

        // Construct children container DOM element.
        this.childrenContainerElement = document.createElement('div');
        this.rootElement.appendChild(this.childrenContainerElement);
        this.setChildrenContainerElementStyle();

        const isLeaf = treeNodeCreationController.isLeaf(path, key, value);

        if (isLeaf === false && plainObject.isPlainObject(value)) {
            for (const [childKey, childValue] of Object.entries(value)) {
                const child = new TreeNode(this, `${path}/${childKey}`, childKey, childValue, treeNodeCreationController);
                this.addChild(child);
            }
        }

        if (parent) {
            // Construct lines DOM elements.
            this.setupLinesElements('#D0D0D0');
        }
    }

    private createTreeNodeContentElement(): HTMLElement {
        return this.treeNodeCreationController.createTreeNodeContentElement(this.path, this.key, this.value);
    }

    private setRootElementStyle(): void {
        this.rootElement.classList.add('treenode-root');
        this.rootElement.style.display = 'grid';

        let height = 4; // Gives a bit of top spacing.
        let childrenOffset = 0;

        if (this.parent) {
            height = TREE_ELEMENT_HEIGHT;
        }
        if (this.parent && this.parent.parent) {
            childrenOffset = HORIZONTAL_LINE_LENGTH;
        }

        this.rootElement.style.gridTemplateRows = `${height}px 1fr`;
        this.rootElement.style.gridTemplateColumns = `${childrenOffset}px ${VERTICAL_BAR_OFFSET}px 1fr`;
    }

    private verticalLineElement: HTMLElement | null = null;

    private setupLinesElements(color: string): void {
        const verticalLineElement = document.createElement('div');
        verticalLineElement.classList.add('treenode-vertical-line');
        verticalLineElement.style.gridColumn = '2';
        verticalLineElement.style.gridRow = '2';
        verticalLineElement.style.width = '100%';
        verticalLineElement.style.borderRight = `1px solid ${color}`;
        this.verticalLineElement = verticalLineElement;
        this.rootElement.appendChild(verticalLineElement);

        if (this.parent && this.parent.parent) {
            const horizontalLineElement = document.createElement('div');
            horizontalLineElement.classList.add('treenode-horizontal-line');
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
        if (!this.titleElement) {
            return;
        }

        this.titleElement.classList.add('treenode-title');
        this.titleElement.style.gridColumn = '2 / span 2';
        this.titleElement.style.gridRow = '1';
        this.titleElement.style.marginLeft = '3px';
    }

    private setChildrenContainerElementStyle(): void {
        this.childrenContainerElement.classList.add('treenode-children-container');
        this.childrenContainerElement.style.gridColumn = '3';
        this.childrenContainerElement.style.gridRow = '2';
    }

    private resetTitle(deepMode: number): void {
        if (this.titleElement) {
            this.titleElement.innerHTML = '';
            this.titleElement.appendChild(this.createTreeNodeContentElement());
        }

        if (deepMode === DEEP_MODE_UP && this.parent) {
            this.parent.resetTitle(deepMode);
        }

        if (deepMode === DEEP_MODE_DOWN) {
            for (const child of this.children) {
                child.resetTitle(deepMode);
            }
        }
    }

    public show(deepMode: number): void {
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

    public hide(deepMode: number): void {
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

    public filter(searchText: string, matchFunction: SearchMatchFunction): void {
        if (!searchText) {
            this.resetTitle(DEEP_MODE_DOWN);
            this.show(DEEP_MODE_DOWN);
            this.updateLines();
            return;
        }

        const markers: PositionMarker[] = [];
        const isMatch = matchFunction(this.key, searchText, markers);

        if (isMatch) {
            if (this.titleElement) {
                this.titleElement.innerHTML = '';
                const title = this.createTreeNodeContentElement();
                this.titleElement.appendChild(TreeNode.createColoredSpan(title.innerText, markers));
            }

            this.show(DEEP_MODE_UP);
            this.show(DEEP_MODE_DOWN);
        } else {
            this.resetTitle(DEEP_MODE_DOWN);
        }

        for (const child of this.children) {
            child.filter(searchText, matchFunction);
        }

        this.updateLines();
    }
}
