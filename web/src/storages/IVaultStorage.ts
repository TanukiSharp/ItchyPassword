export interface IVaultStorage {
    clear(): void;
    getVaultContent(): Promise<string | null>;
    setVaultContent(newContent: string | null, updateMessage: string): Promise<boolean>;
    getVaultSettings(): string
}
