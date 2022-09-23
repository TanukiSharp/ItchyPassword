export interface IComponent {
    readonly name: string;
    getVaultHint(): string;
    init(): void;
}
