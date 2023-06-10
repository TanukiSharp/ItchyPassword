export class LogicalTreeNode {
    private parent: LogicalTreeNode | null = null;
    private readonly children: LogicalTreeNode[] = [];

    public get Parent(): LogicalTreeNode | null {
        return this.parent;
    }

    public get Children(): LogicalTreeNode[] {
        return this.children;
    }

    public Content?: any;

    public addChild(child: LogicalTreeNode): LogicalTreeNode {
        if (child.Parent != null)
            throw new Error('Child already has parent.');

        if (this.children.includes(child))
            throw new Error('Child already in children.');

        child.parent = this;
        this.children.push(child);

        return child;
    }
}
