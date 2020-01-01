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

function createSafeTimeout(f: Function, duration: number): Function {
    let timeout: number | undefined;
    return () => {
        if (timeout) {
            clearTimeout(timeout);
        }
        timeout = setTimeout(f, duration);
    };
}

export function setupCopyButton(txt: HTMLInputElement, button: HTMLInputElement): void {
    const setupStopAnimationTimer = createSafeTimeout(() => {
        button.classList.remove('good-flash');
        button.classList.remove('bad-flash');
    }, 1000);

    button.addEventListener('click', async () => {
        if (await writeToClipboard(txt.value)) {
            button.classList.add('good-flash');
        } else {
            button.classList.add('bad-flash');
        }
        setupStopAnimationTimer();
    });
}

export const SUCCESS_COLOR: string = '#D0FFD0';
export const ERROR_COLOR: string = '#FFD0D0';
