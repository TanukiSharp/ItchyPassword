export const SUCCESS_COLOR: string = '#D0FFD0';
export const ERROR_COLOR: string = '#FFD0D0';

export function getElementById(elementName: string): HTMLElement {
    const element: HTMLElement|null = document.getElementById(elementName);

    if (elementName === null) {
        throw new Error(`DOM element '${elementName}' not found.`);
    }

    return element as HTMLElement;
}

export async function writeToClipboard(text: string, logFunc?: (..._: any[]) => void): Promise<boolean> {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (error) {
        const typedError = error as Error;
        console.error(typedError.stack || error);
        logFunc?.(typedError.stack || error);
        return false;
    }
}

export function clearText(txt: HTMLInputElement, refocus: boolean = false): void {
    txt.value = '';
    if (refocus) {
        txt.focus();
    }
}

interface ThrottleTimeout {
    start: Function;
    end: Function;
}

function createThrottleTimeout(clearFunc: Function, duration: number): ThrottleTimeout {
    let timeout: number | undefined = undefined;

    return {
        start: () => {
            if (timeout !== undefined) {
                clearTimeout(timeout);
                timeout = undefined;
            }
            clearFunc();
        },
        end: () => {
            if (timeout !== undefined) {
                clearTimeout(timeout);
            }
            timeout = setTimeout(clearFunc, duration);
        }
    };
}

export type FeedbackButtonAsyncFunction = () => Promise<boolean> | boolean | Promise<void> | void;

export function setupFeedbackButton(button: HTMLButtonElement, action: FeedbackButtonAsyncFunction, logError?: (error: any) => any): () => void {
    const throttleTimeout: ThrottleTimeout = createThrottleTimeout(() => {
        button.classList.remove('good-flash');
        button.classList.remove('bad-flash');
    }, 1000);

    const clickFunction = async () => {
        button.disabled = true;
        throttleTimeout.start();

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
            const typedError = error as Error;
            button.classList.add('bad-flash');
            console.error(typedError.stack || error);
            logError?.(typedError.stack || error);
        } finally {
            throttleTimeout.end();
            button.disabled = false;
        }
    };

    button.addEventListener('click', clickFunction);

    return clickFunction;
}

export function setupCopyButton(txt: HTMLInputElement, button: HTMLButtonElement, logFunc?: (..._: any[]) => void): () => void {
    return setupFeedbackButton(button, () => writeToClipboard(txt.value), logFunc);
}

export function setupViewButton(txt: HTMLInputElement, button: HTMLButtonElement): void {
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

export function setupShowHideButton(button: HTMLButtonElement, startVisible: boolean, elements: HTMLElement[]): void {
    let isVisible = startVisible;
    button.addEventListener('click', function () {
        isVisible = !isVisible;
        showHideMany(elements, isVisible);
    });
    showHideMany(elements, isVisible);
}
