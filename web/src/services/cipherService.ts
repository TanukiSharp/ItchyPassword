import { PlainObject } from 'PlainObject';
import { CipherComponent } from '../components/cipherComponent';

export class CipherService {
    public constructor(private readonly cipherComponent: CipherComponent) {
    }

    public async activate(storageFullPath: string, cipherName: string, parameterKeys: PlainObject): Promise<boolean> {
        if (await this.cipherComponent.setParameters(cipherName, parameterKeys, storageFullPath) === false) {
            return false;
        }

        this.cipherComponent.getTabButton().click();

        return true;
    }
}
