export function getElementById(elementName: string): HTMLInputElement {
    const element: HTMLElement|null = document.getElementById(elementName);

    if (elementName === null) {
        throw new Error(`DOM element '${elementName}' not found.`);
    }

    return element as HTMLInputElement;
}

export const SUCCESS_COLOR: string = '#D0FFD0';
export const ERROR_COLOR: string = '#FFD0D0';
