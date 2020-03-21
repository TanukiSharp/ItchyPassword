export class TaskCancelledError extends Error {
    private _name: string;

    public get name(): string {
        return this._name;
    }

    constructor(message?: string) {
        super(message);
        this._name = TaskCancelledError.ERROR_NAME;
        Object.setPrototypeOf(this, new.target.prototype);
    }

    public static readonly ERROR_NAME: string = 'TaskCancelledError';

    public static isMatching(error: Error) {
        return error && error.name === TaskCancelledError.ERROR_NAME;
    }
}

export class CancellationTokenSource {
    private _isCancelled: boolean = false;
    private _token: CancellationToken;

    constructor() {
        this._token = new CancellationToken(this);
    }

    public get isCancelled(): boolean {
        return this._isCancelled;
    }

    public get token(): CancellationToken {
        return this._token;
    }

    public cancel(): void {
        this._isCancelled = true;
    }
}

export class CancellationToken {
    private static readonly _none: CancellationToken = new CancellationToken(new CancellationTokenSource());
    public static get none(): CancellationToken {
        return CancellationToken._none;
    }

    constructor(private source: CancellationTokenSource) {
    }

    public get isCancelled(): boolean {
        return this.source.isCancelled;
    }
}

export function ensureNotCancelled(cancellationToken: CancellationToken): void {
    if (cancellationToken.isCancelled) {
        throw new TaskCancelledError();
    }
}

export function rethrowCancelled(error: Error): void {
    if (TaskCancelledError.isMatching(error)) {
        throw error;
    }
}

export type TaskFactory<T> = (cancellationToken: CancellationToken) => Promise<T>;

// Manages the lifetime of a single task, and automatically cancels the previous when running a new one.
// It also awaits for previous task to be fully terminated before running the new one.
export class TaskRunner<TValue> {
    private currentTokenSource: CancellationTokenSource | null = null;
    private currentTask: Promise<TValue | undefined> | null = null;
    private microThreadId: number = 0;

    // Gets a value indicating whether a task is currently running or not.
    public get isRunning(): boolean {
        return this.currentTask !== null;
    }

    // Cancels the currently running task, if any.
    // Returns true if no one called cancelInternal() when it returns.
    private async cancelInternal(throwTaskCanceledError: boolean): Promise<boolean> {
        if (this.microThreadId === Number.MAX_SAFE_INTEGER) {
            this.microThreadId = 0;
        } else {
            this.microThreadId = this.microThreadId + 1;
        }

        const localMicroThreadId: number = this.microThreadId;

        // This corresponds to the end of cancelAndExecute().
        if (this.currentTask === null) {
            return true;
        }

        if (this.currentTokenSource !== null) {
            this.currentTokenSource.cancel();

            // The above currentTokenSource.cancel() can run the finally block of cancelAndExecute() and set currentTask to null.
            if (this.currentTask !== null) {
                try {
                    await this.currentTask;
                } catch (error) {
                    if (TaskCancelledError.isMatching(error)) {
                        if (throwTaskCanceledError) {
                            throw error;
                        }
                    } else {
                        throw error;
                    }
                }
            }
        }

        return localMicroThreadId === this.microThreadId;
    }

    // Cancels the currently running task, if any.
    // throwTaskCanceledError: Pass true allow to throw a TaskCancelledError.
    // Returns a Promise that completes when the current job is fully cancelled.
    public async cancel(throwTaskCanceledError: boolean = false): Promise<void> {
        await this.cancelInternal(throwTaskCanceledError);
    }

    // Cancels the current task if any, and runs a new one.
    // T: Type of value returned by the task to run.
    // taskFactory: A task producer that receives a CancellationToken.
    // throwTaskCanceledError: Pass true allow to throw a TaskCancelledError.
    // Returns the task produced by the taskFactory.
    public async cancelAndExecute(taskFactory: TaskFactory<TValue>, throwTaskCanceledError: boolean = false): Promise<TValue | undefined> {
        if (await this.cancelInternal(throwTaskCanceledError) === false) {
            if (throwTaskCanceledError === false) {
                return undefined;
            }
            throw new TaskCancelledError();
        }

        var localToken = new CancellationTokenSource();
        this.currentTokenSource = localToken;

        try {
            this.currentTask = taskFactory(this.currentTokenSource.token);
            return await this.currentTask;
        } catch (error) {
            if (TaskCancelledError.isMatching(error) && throwTaskCanceledError === false) {
                return undefined;
            }
            throw error;
        } finally {
            this.currentTask = null;
        }
    }
}
