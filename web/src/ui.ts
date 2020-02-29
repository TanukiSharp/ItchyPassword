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

export type FeedbackButtonAsyncFunction = () => Promise<boolean> | boolean | Promise<void> | void;

export function setupFeedbackButton(button: HTMLInputElement, action: FeedbackButtonAsyncFunction): void {
    const setupStopAnimationTimer = createSafeTimeout(() => {
        button.classList.remove('good-flash');
        button.classList.remove('bad-flash');
    }, 1000);

    button.addEventListener('click', async () => {
        button.disabled = true;

        try {
            const actionResult = action();

            let result;
            if (actionResult instanceof Promise) {
                result = await actionResult;
            } else {
                result = actionResult;
            }

            if (result === undefined || result === true) {
                button.classList.add('good-flash');
            } else {
                button.classList.add('bad-flash');
            }
        } catch (error) {
            button.classList.add('bad-flash');
            console.error(error.message || error);
        } finally {
            setupStopAnimationTimer();
            button.disabled = false;
        }
    });
}

export function setupCopyButton(txt: HTMLInputElement, button: HTMLInputElement): void {
    setupFeedbackButton(button, () => writeToClipboard(txt.value));
}

export function setupViewButton(txt: HTMLInputElement, button: HTMLInputElement): void {
    button.addEventListener('click', () => {
        if (txt.type === 'password') {
            txt.type = 'input';
            button.innerHTML = 'Hide';
        } else {
            txt.type = 'password';
            button.innerHTML = 'View';
        }
    });
}

export function showHide(element: HTMLElement, isVisible: boolean): void {
    if (isVisible) {
        element.style.removeProperty('display');
    } else {
        element.style.setProperty('display', 'none');
    }
}

export function showHideMany(elements: HTMLElement[], isVisible: boolean): void {
    for (const element of elements) {
        showHide(element, isVisible);
    }
}

export function setupShowHideButton(button: HTMLInputElement, startVisible: boolean, elements: HTMLElement[]): void {
    let isVisible = startVisible;
    button.addEventListener('click', function () {
        isVisible = !isVisible;
        showHideMany(elements, isVisible);
    });
    showHideMany(elements, isVisible);

}

export const SUCCESS_COLOR: string = '#D0FFD0';
export const ERROR_COLOR: string = '#FFD0D0';
