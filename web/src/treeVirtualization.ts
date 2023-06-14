import { LogicalTreeNode } from './components/vaultComponents/logicalTreeNode';
import { VisualTreeNode } from './components/vaultComponents/visualTreeNode';
import * as plainObject from './PlainObject';

const createObject = function(): any {
    return {
        n1: {
            n11: {
                n112: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n113: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n114: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
            },
            n12: {
                n121: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n122: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n123: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
            },
        },
        n2: {
            n21: {
                n211: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n212: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
            },
            n22: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n23: {
                n231: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n232: {
                    a: 'aaa',
                    b: 'bbb',
                    c: 'ccc',
                },
                n233: {
                    n2331: {
                        a: 'aaa',
                        b: 'bbb',
                        c: 'ccc',
                    },
                    n2332: {
                        a: 'aaa',
                        b: 'bbb',
                        c: 'ccc',
                    },
                },
            },
        },
        n3: {
            n31: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n32: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n33: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n34: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
        },
        n4: {
            n41: {
                n411: {
                    n4111: {
                        a: 'aaa',
                        b: 'bbb',
                        c: 'ccc',
                    },
                    n4112: {
                        a: 'aaa',
                        b: 'bbb',
                        c: 'ccc',
                    },
                },
            },
        },
        n5: {
            n51: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n52: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
            n53: {
                a: 'aaa',
                b: 'bbb',
                c: 'ccc',
            },
        },
    }
}

export const testTreeVirtualization = function() {
    const root = new LogicalTreeNode();
    root.Content = '<root>';
    buildTree(root, createObject());

    const vtn = new VisualTreeNode(root);

    const list = vtn.toList();

    for (let i = 1; i < list.length; i++) {
        const child = list[i];

        let msg = '';
        for (let j = 1; j < child.HierarchyDecorations.length; j++) {
            msg += child.HierarchyDecorations[j];
        }
        msg += child.LogicalTreeNode.Content;
        console.log(msg);
    }
}

const buildTree = function(node: LogicalTreeNode, obj: any) {
    if (plainObject.isPlainObject(obj)) {
        for (const [key, value] of Object.entries(obj)) {
            const child = new LogicalTreeNode();
            child.Content = key;
            buildTree(child, value);
            node.addChild(child);
        }
    } else {
        node.Content = `${node.Content}: ${obj}`;
    }
}
