import { VaultComponent } from '../components/vaultComponent';

export class VaultService {
    private readonly vaultComponent: VaultComponent;

    public constructor(vaultComponent: VaultComponent) {
        this.vaultComponent = vaultComponent;
    }

    public computeUserPathMatchDepth(path: string): number {
        return this.vaultComponent.computeUserPathMatchDepth(path);
    }
}
