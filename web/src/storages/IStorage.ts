export interface IStorage {
    getVaultContent(): Promise<string | null>;
    setVaultContent(newContent: string | null, updateMessage: string): Promise<boolean>;
}
