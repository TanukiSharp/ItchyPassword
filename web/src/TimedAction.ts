export class TimedAction {
    private timeout: number | undefined;
    public constructor(private action: Function, private delay: number) {
    }

    public reset(overrideDelay: number | undefined = undefined): void {
        if (this.timeout !== undefined) {
            clearTimeout(this.timeout);
        }

        const delay = overrideDelay !== undefined ? overrideDelay : this.delay;

        this.timeout = window.setTimeout(() => {
            this.action();
            this.timeout = undefined;
        }, delay);
    }
}
