import { SecureLocalStorage } from './SecureLocalStorage';
import { IVaultStorage } from './IVaultStorage';
import { PlainObject } from '../PlainObject';

interface IGitHubContent {
    sha: string;
    content: string;
}

abstract class GitHubVaultStorageBase implements IVaultStorage {
    static BASE_URL: string = 'https://api.github.com';
    static AUTH_TOKEN_KEY_NAME: string = 'GitHubVaultStorageBase.AuthToken';

    static LOCAL_STORAGE_KEY_USERNAME: string = 'GitHubVaultStorageBase.Username';
    static LOCAL_STORAGE_KEY_REPO: string = 'GitHubVaultStorageBase.Repository';
    static LOCAL_STORAGE_KEY_FILENAME: string = 'GitHubVaultStorageBase.Filename';

    private token: string | null = null;
    private oneTimePassword: string | null = null;
    private currentVaultContentHash: string | null = null;

    private username: string | null = null;
    private repositoryName: string | null = null;
    private vaultFilename: string | null = null;

    protected getUsername(): string | null {
        return this.username;
    }

    protected getRepositoryName(): string | null {
        return this.repositoryName;
    }

    protected getVaultFilename(): string | null {
        return this.vaultFilename;
    }

    public constructor(protected secureLocalStorage: SecureLocalStorage) {
    }

    public clear(): void {
        this.secureLocalStorage.removeItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_USERNAME);
        this.secureLocalStorage.removeItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_REPO);
        this.secureLocalStorage.removeItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_FILENAME);

        this.secureLocalStorage.removeItem(GitHubVaultStorageBase.AUTH_TOKEN_KEY_NAME);
    }

    private constructTokenAuthString(): string {
        return `token ${this.token}`;
    }

    private constructFetchRequest(method: string, authHeader: string, body: any): RequestInit {
        const headers: PlainObject = {
            'Accept': 'application/vnd.github.v3+json',
            'Content-Type': 'application/json',
            'Authorization': authHeader
        };

        if (this.oneTimePassword) {
            headers['x-github-otp'] = this.oneTimePassword;
        }

        return {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined
        };
    }

    private constructUrl(relativeUrl: string): string {
        return `${GitHubVaultStorageBase.BASE_URL}${relativeUrl}`;
    }

    protected async request(retryOnUnauthorized: boolean, method: string, relativeUrl: string, authHeader: string, body: any = undefined): Promise<Response | null> {
        const url: string = this.constructUrl(relativeUrl);
        const requestInfo: RequestInit = this.constructFetchRequest(method, authHeader, body);

        let response: Response = await fetch(url, requestInfo);

        if (response.status === 401 && retryOnUnauthorized) {
            this.oneTimePassword = prompt('Input your 2FA code:');

            if (!this.oneTimePassword) {
                return null;
            }

            return await this.request(retryOnUnauthorized, method, relativeUrl, authHeader, body);
        }

        return response;
    }

    protected getSetVaultParameter(key: string, promptText: string, defaultValue?: string): string | null {
        let value: string | null = window.localStorage.getItem(key);

        if (value) {
            return value;
        }

        value = prompt(promptText, defaultValue);

        if (!value) {
            return null;
        }

        window.localStorage.setItem(key, value);

        return value;
    }

    protected ensureVaultParameters(): Promise<boolean> {
        const url = new URL(window.location.toString());

        let defaultAccountUsername = '';
        let defaultRepo = '';

        if (url.hostname === 'github.com') {
            const pathElements = url.pathname.split('/');
            if (pathElements.length >= 3) {
                defaultAccountUsername = pathElements[1];
                defaultRepo = `${pathElements[2]}Vault`;
            }
        }

        if (!defaultRepo) {
            defaultRepo = 'ItchyPasswordVault';
        }

        const username = this.getSetVaultParameter(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_USERNAME, 'GitHub account username:', defaultAccountUsername);
        if (!username) {
            return Promise.resolve(false);
        }
        this.username = username;

        const repositoryName: string | null = this.getSetVaultParameter(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_REPO, 'Vault GitHub repository name:', defaultRepo);
        if (!repositoryName) {
            return Promise.resolve(false);
        }
        this.repositoryName = repositoryName;

        const vaultFilename: string | null = this.getSetVaultParameter(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_FILENAME, 'Vault filename:', 'vault.json');
        if (!vaultFilename) {
            return Promise.resolve(false);
        }
        this.vaultFilename = vaultFilename;

        return Promise.resolve(true);
    }

    protected abstract getToken(): Promise<string | null>;

    private async ensureToken(): Promise<boolean> {
        let token: string | null = await this.secureLocalStorage.getItem(GitHubVaultStorageBase.AUTH_TOKEN_KEY_NAME);

        if (token === null) {
            token = await this.getToken();

            if (token === null) {
                return false;
            }
        }

        await this.secureLocalStorage.setItem(GitHubVaultStorageBase.AUTH_TOKEN_KEY_NAME, token);

        this.token = token;

        return true;
    }

    private constructVaultFileUrl() {
        return `/repos/${this.username}/${this.repositoryName}/contents/${this.vaultFilename}`;
    }

    public async getVaultContent(): Promise<string | null> {
        if (await this.ensureVaultParameters() === false) {
            return null;
        }
        if (await this.ensureToken() === false) {
            return null;
        }

        const url: string = this.constructVaultFileUrl();
        const response: Response | null = await this.request(false, 'GET', url, this.constructTokenAuthString());

        if (response === null) {
            console.warn('Fetching vault content aborted.');
            return null;
        }

        if (response.ok === false) {
            if (response.status === 401) {
                this.secureLocalStorage.removeItem(GitHubVaultStorageBase.AUTH_TOKEN_KEY_NAME);
                this.token = null;
                this.oneTimePassword = null;

                return await this.getVaultContent();
            } else if (response.status === 404) {
                if (await this.setVaultContent('{}', '[ItchyPassword] Creation of vault file')) {
                    return '{}';
                }
                return null;
            }

            console.error(`Failed to fetch vault file '${this.vaultFilename}'.`, response);

            return null;
        }

        const responseContent: IGitHubContent = await response.json();

        this.currentVaultContentHash = responseContent.sha;

        const decodedContent = atob(responseContent.content);

        if (decodedContent.trim() === '') {
            return '{}';
        }

        return decodedContent;
    }

    public async setVaultContent(newContent: string, updateMessage: string): Promise<boolean> {
        if (await this.ensureVaultParameters() === false) {
            return false;
        }
        if (await this.ensureToken() === false) {
            return false;
        }

        const body = {
            message: updateMessage,
            content: btoa(newContent),
            sha: this.currentVaultContentHash
        };

        const url: string = this.constructVaultFileUrl();
        const response: Response | null = await this.request(false, 'PUT', url, this.constructTokenAuthString(), body);

        if (response === null) {
            console.warn('Push new vault content aborted.');
            return false;
        }

        const responseContent: any = await response.json();

        if (response.ok === false) {
            console.error(`Failed to create/update vault file '${this.vaultFilename}'.`, response, responseContent);
            return false;
        }

        this.currentVaultContentHash = (responseContent.content as IGitHubContent).sha;

        return true;
    }

    public getVaultSettings(): string {
        const username = window.localStorage.getItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_USERNAME);
        const repositoryName = window.localStorage.getItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_REPO);
        const vaultFilename = window.localStorage.getItem(GitHubVaultStorageBase.LOCAL_STORAGE_KEY_FILENAME);

        const usernameDisplay = username == null ? '<null>' : `'${username}'`;
        const repositoryNameDisplay = repositoryName == null ? '<null>' : `'${repositoryName}'`;
        const vaultFilenameDisplay = vaultFilename == null ? '<null>' : `'${vaultFilename}'`;

        return `username: ${usernameDisplay}\nrepository name: ${repositoryNameDisplay}\nvault filename: ${vaultFilenameDisplay}`;
    }
}

// ================================================================================================

export class GitHubPersonalAccessTokenVaultStorage extends GitHubVaultStorageBase {
    protected getToken(): Promise<string | null> {
        const authToken: string | null = prompt('Personal access token:');
        return Promise.resolve(authToken);
    }
}
