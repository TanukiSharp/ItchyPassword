import { CancellationToken } from '../asyncUtils';
import * as stringUtils from '../stringUtils';
import * as ui from '../ui';
import * as passwordComponent from '../components/passwordComponent';
import * as serviceManager from './serviceManger';

export class PasswordService {
    async generateAndCopyPasswordToClipboard(publicPart: string, alphabet?: string, length?: number): Promise<boolean> {
        alphabet = alphabet !== undefined ? alphabet : passwordComponent.DEFAULT_ALPHABET;
        length = length !== undefined ? length : passwordComponent.DEFAULT_LENGTH;

        const keyString: string | null = await passwordComponent.generatePasswordString(publicPart, alphabet, CancellationToken.none);

        if (keyString === null) {
            return false;
        }

        const password = stringUtils.truncate(keyString, Math.max(4, length));

        const errorLogsService = serviceManager.getService('errorLogs');
        const logFunc = errorLogsService.createLogErrorMessageFunction();

        return await ui.writeToClipboard(password, logFunc);
    }
}
