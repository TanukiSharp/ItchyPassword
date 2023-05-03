import { PlainObject } from 'PlainObject';
import { CipherComponent, findLatestCipher } from '../components/cipherComponent';

export class CipherService {
    private latestVersion: number;

    public constructor(private readonly cipherComponent: CipherComponent) {
        this.latestVersion = findLatestCipher().version
    }

    public isLatestVersion(version: number): boolean {
        return version === this.getLatestVersion();
    }

    public getLatestVersion(): number {
        return this.latestVersion;
    }

    public async activate(storageFullPath: string, cipherName: string, parameterKeys: PlainObject): Promise<boolean> {
        if (await this.cipherComponent.setParameters(cipherName, parameterKeys, storageFullPath) === false) {
            return false;
        }

        this.cipherComponent.getTabButton().click();

        return true;
    }
}
