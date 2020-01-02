export interface IVaultStorage {
    getVaultContent(): Promise<string | null>;
    setVaultContent(newContent: string | null, updateMessage: string): Promise<boolean>;
}
