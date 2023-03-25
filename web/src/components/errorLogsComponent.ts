import { getElementById } from '../ui';
import { ITabInfo } from '../TabControl';
import * as serviceManager from '../services/serviceManger';
import { ErrorLogsService } from '../services/errorLogsService';
import * as storageOutputComponent from './storageOutputComponent';

const divErrorLogs = getElementById('divErrorLogs');
const btnTabErrorLogs = getElementById('btnTabErrorLogs') as HTMLButtonElement;

const btnClearErrorLogs = getElementById('btnClearErrorLogs') as HTMLButtonElement;
const txtErrorLogs = getElementById('txtErrorLogs') as HTMLTextAreaElement;

export class ErrorLogsComponent implements ITabInfo {
    constructor() {
        window.addEventListener('error', (e) => this.onError(e), true);
        window.addEventListener('unhandledrejection', (e) => this.onUnhandledRejection(e), true);

        btnClearErrorLogs.addEventListener('click', () => {
            txtErrorLogs.value = '';
        });

        serviceManager.registerService('errorLogs', new ErrorLogsService(this));
    }

    public logErrorMessage(...args: any[]): void {
        if (args.length == 0) {
            return;
        }

        const now = new Date().toISOString();

        let message = args[0].toString();

        for (let i = 1; i < args.length; i++) {
            message += ` ${args[i].toString()}`;
        }

        txtErrorLogs.value += `[${now}] ${message}\n\n`;
    }

    public onUnhandledRejection(errorEvent: PromiseRejectionEvent): void {
        this.logErrorMessage(`Promise rejected, reason: ${errorEvent.reason}`);
    }

    public onError(errorEvent: ErrorEvent): void {
        this.logErrorMessage(`${errorEvent.message}\n${errorEvent.toString()}`);
    }

    getTabButton(): HTMLButtonElement {
        return btnTabErrorLogs;
    }

    getTabContent(): HTMLElement {
        return divErrorLogs;
    }

    onTabSelected(): void {
        storageOutputComponent.hide();
    }
}
