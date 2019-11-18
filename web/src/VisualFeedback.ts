export class VisualFeedback {
    private element: HTMLElement;
    private timeout: number | undefined;

    public constructor(element: HTMLElement) {
        this.element = element;
    }

    public setText(text: string, duration: number) {
        this.element.innerHTML = text;
        if (this.timeout) {
            clearTimeout(this.timeout);
        }
        this.timeout = setTimeout(() => this.element.innerHTML = '', duration);
    }
}
