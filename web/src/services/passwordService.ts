import { CancellationToken } from '../asyncUtils';
import * as stringUtils from '../stringUtils';
import * as ui from '../ui';
import { PasswordComponent, generatePasswordString, run } from '../components/passwordComponent';
import { DEFAULT_ALPHABET, DEFAULT_LENGTH, CURRENT_PASSWORD_GENERATOR_VERSION } from '../components/passwordComponent';
import * as serviceManager from './serviceManger';
import { PlainObject } from 'PlainObject';

export class PasswordService {
    constructor(private readonly passwordComponent: PasswordComponent) {
    }

    isLatestVersion(version: number): boolean {
        return version === this.getLatestVersion();
    }

    getLatestVersion(): number {
        return CURRENT_PASSWORD_GENERATOR_VERSION;
    }

    async generateAndCopyPasswordToClipboard(publicPart: string, alphabet?: string, length?: number, version?: number): Promise<boolean> {
        alphabet = alphabet !== undefined ? alphabet : DEFAULT_ALPHABET;
        length = length !== undefined ? length : DEFAULT_LENGTH;
        version = version !== undefined ? version : CURRENT_PASSWORD_GENERATOR_VERSION;

        const keyString: string | null = await generatePasswordString(publicPart, alphabet, version, CancellationToken.none);

        if (keyString === null) {
            return false;
        }

        const password = stringUtils.truncate(keyString, Math.max(4, length));

        const errorLogsService = serviceManager.getService('errorLogs');
        const logFunc = errorLogsService.createLogErrorMessageFunction();

        return await ui.writeToClipboard(password, logFunc);
    }

    public async activate(storageFullPath: string, parameterKeys: PlainObject): Promise<boolean> {
        if (this.passwordComponent.setParameters(parameterKeys, storageFullPath) === false) {
            return false;
        }

        await run();

        this.passwordComponent.getTabButton().click();

        return true;
    }
}
