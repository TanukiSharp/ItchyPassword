import { LogicalTreeNode } from './logicalTreeNode';

export class VisualTreeNode {
    private isVisible: boolean = true;
    private isExpanded: boolean = true;
    private depth: number = 0;
    private hierarchyDecorations!: string[];
    private parent: VisualTreeNode | null = null;
    private readonly logicalTreeNode: LogicalTreeNode;
    private children!: VisualTreeNode[];
    private isLastChild!: boolean;

    public get IsVisible(): boolean {
        return this.isVisible;
    }
    public set IsVisible(value: boolean) {
        this.isVisible = value;
    }

    public get IsExpanded(): boolean {
        return this.isExpanded;
    }
    public set IsExpanded(value: boolean) {
        this.isExpanded = value;
    }

    public get HierarchyDecorations(): string[] {
        return this.hierarchyDecorations;
    }

    public get Depth(): number {
        return this.depth;
    }

    public get Parent(): VisualTreeNode | null {
        return this.parent;
    }

    public get LogicalTreeNode(): LogicalTreeNode {
        return this.logicalTreeNode;
    }

    public get Children(): VisualTreeNode[] {
        return this.children;
    }

    constructor(logicalTreeNode: LogicalTreeNode) {
        this.logicalTreeNode = logicalTreeNode;
        this.init(null, true);
    }

    private init(parentVisualTreeNode: VisualTreeNode | null, isLastChild: boolean) {
        this.isLastChild = isLastChild;

        this.parent = parentVisualTreeNode;
        this.depth = 0;

        if (parentVisualTreeNode !== null) {
            this.depth = parentVisualTreeNode.depth + 1;
        }

        this.hierarchyDecorations = new Array(this.depth);

        const count = this.logicalTreeNode.Children.length;
        const last = count - 1;

        this.children = new Array(count);

        for (let i = 0; i < count; i++)
        {
            const isChildLastChild = i == last;

            const child = new VisualTreeNode(this.logicalTreeNode.Children[i]);
            child.init(this, isChildLastChild);

            this.children[i] = child;
        }

        this.setupHierarchyDecorations();
    }

    private setupHierarchyDecorations() {
        let i = this.hierarchyDecorations.length - 1;

        if (this.parent != null)
        {
            if (this.isLastChild)
                this.hierarchyDecorations[i--] = '\u2514'; // └
            else
                this.hierarchyDecorations[i--] = '\u251c'; // ├
        }

        let parent: VisualTreeNode | null = this.parent;

        while (parent !== null && parent.Parent !== null) {
            if (parent.isLastChild) {
                this.hierarchyDecorations[i--] = ' ';
            } else {
                this.hierarchyDecorations[i--] = '\u2502'; // │
            }

            parent = parent.Parent;
        }
    }

    public toList(): VisualTreeNode[] {
        const list: VisualTreeNode[] = [];

        this.toListCore(list);

        return list;
    }

    private toListCore(list: VisualTreeNode[]): void {
        if (this.IsVisible === false) {
            return;
        }

        list.push(this);

        if (this.IsExpanded) {
            for (const child of this.Children) {
                child.toListCore(list);
            }
        }
    }

    public toString(): string {
        return this.toStringOptions(VisualTreeNodePrintOptions.Default);
    }

    public toStringOptions(printOptions: VisualTreeNodePrintOptions): string {
        let result = { value: '' };

        this.toString2(result, printOptions);

        return result.value;
    }

    private toString2(result: { value: string }, printOptions: VisualTreeNodePrintOptions) {
        this.appendHierarchyDecorations(result, printOptions.Width, printOptions.AppendSpace);

        const str: string = this.logicalTreeNode.Content.toString();
        result.value += str.substring(0, Math.min(str.length, 50));

        result.value += '\n';

        if (this.children.length > 0)
        {
            for (const child of this.children) {
                child.toString2(result, printOptions);
            }
        }
    }

    private appendHierarchyDecorations(result: { value: string }, width: number, appendSpace: boolean) {
        for (const hierarchyDecoration of this.hierarchyDecorations) {
            switch (hierarchyDecoration)
            {
                case ' ':
                    result.value += ' '.repeat(width);
                    break;
                case '\u251c': // ├
                    result.value += hierarchyDecoration;
                    if (width > 1) {
                        result.value += '\u2500'.repeat(width - 1);
                    }
                    break;
                case '\u2502': // │
                    result.value += hierarchyDecoration;
                    if (width > 1) {
                        result.value += ' '.repeat(width - 1);
                    }
                    break;
                case '\u2514': // └
                    result.value += hierarchyDecoration;
                    if (width > 1) {
                        result.value += '\u2500'.repeat(width - 1);
                    }
                    break;
            }

            if (appendSpace) {
                result.value += ' ';
            }
        }
    }
}

export class VisualTreeNodePrintOptions {
    public readonly Width: number;
    public readonly AppendSpace: boolean;

    public static readonly Default: VisualTreeNodePrintOptions = new VisualTreeNodePrintOptions(2, true);

    constructor(width: number, appendSpace: boolean)
    {
        if (width < 1)
            throw new Error(`Argument 'width' invalid. Must be greater than or equal to 1, received ${width}.`);

        this.Width = width;
        this.AppendSpace = appendSpace;
    }
}
