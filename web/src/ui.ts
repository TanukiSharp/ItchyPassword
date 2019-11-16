import VisualFeedback from './VisualFeedback';

export function getElementById(elementName: string): HTMLInputElement {
    const element: HTMLElement|null = document.getElementById(elementName);

    if (elementName === null) {
        throw new Error(`DOM element '${elementName}' not found.`);
    }

    return element as HTMLInputElement;
}

async function writeToClipboard(text: string): Promise<boolean> {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (error) {
        console.error(error.stack || error);
        return false;
    }
}

export function setupCopyButton(txt: HTMLInputElement, button: HTMLInputElement, feedback: HTMLInputElement): void {
    const visualFeedback: VisualFeedback = new VisualFeedback(feedback);
    button.addEventListener('click', async () => {
        if (await writeToClipboard(txt.value)) {
            visualFeedback.setText('Copied', 3000);
        } else {
            visualFeedback.setText('<span style="color: red">Failed to copy</span>', 3000);
        }
    });
}

export const SUCCESS_COLOR: string = '#D0FFD0';
export const ERROR_COLOR: string = '#FFD0D0';
