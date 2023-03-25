import { ErrorLogsComponent } from 'components/errorLogsComponent';

export class ErrorLogsService {
    constructor(private errorLogsComponent: ErrorLogsComponent) {
    }

    public createLogErrorMessageFunction(): (...args: any[]) => void {
        return (args) => this.logErrorMessage(args);
    }

    public logErrorMessage(...args: any[]): void {
        this.errorLogsComponent.logErrorMessage(...args);
    }
}
