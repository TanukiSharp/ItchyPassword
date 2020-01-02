import { IVaultStorage } from './IVaultStorage';
import { PlainObject } from '../PlainObject';

interface IApp {
    name: string;
}

interface IAuthorization {
    id: number;
    app: IApp;
}

interface IGitHubContent {
    sha: string;
    content: string;
}

export interface IKeyValueStorage {
    removeValue(key: string): void;
    getValue(key: string): Promise<string | null>;
    setValue(key: string, value: string): Promise<void>;
}

export class GitHubVaultStorage implements IVaultStorage {
    static BASE_URL: string = 'https://api.github.com';
    static AUTHORIZATION_NAME: string = 'github.com/TanukiSharp/ItchyPassword';
    static KEY_VALUE_STORAGE_TOKEN_KEY_NAME: string = 'GitHubVaultStorage.Token';

    private repositoryOwner: string;
    private basicAuthHeader: string;

    private token: string | null = null;
    private oneTimePassword: string | null = null;
    private currentVaultContentHash: string | null = null;

    public constructor(username: string, password: string, private repositoryName: string, private vaultFilename: string, private keyValueStorage: IKeyValueStorage) {
        this.repositoryOwner = username;
        this.basicAuthHeader = this.constructBasicAuthString(username, password);
    }

    private constructBasicAuthString(username: string, password: string): string {
        const authString = btoa(`${username}:${password}`);
        return `Basic ${authString}`;
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
        return `${GitHubVaultStorage.BASE_URL}${relativeUrl}`;
    }

    private async request(retryOnUnauthorized: boolean, method: string, relativeUrl: string, authHeader: string, body: any = undefined): Promise<Response | null> {
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

    private async listAuthorizations(): Promise<IAuthorization[] | null> {
        const response: Response | null = await this.request(true, 'GET', '/authorizations', this.basicAuthHeader);

        if (response === null) {
            console.warn('List authorizations aborted.');
            return null;
        }

        if (response.ok === false) {
            console.error('Failed to list authorizations.', response);
            return null;
        }

        return await response.json();
    }

    private async deleteAuthorization(authorization: IAuthorization): Promise<boolean> {
        const response: Response | null = await this.request(true, 'DELETE', `/authorizations/${authorization.id}`, this.basicAuthHeader);

        if (response === null) {
            console.warn('Delete authorization aborted.');
            return false;
        }

        if (response.ok === false) {
            console.error(`Failed to delete authorization '${authorization.id}'.`, response);
        }

        return response.ok;
    }

    private async createAuthorization(): Promise<string | null> {
        const body: PlainObject = {
            scopes: ['repo'],
            note: GitHubVaultStorage.AUTHORIZATION_NAME
        };

        const response: Response | null = await this.request(true, 'POST', '/authorizations', this.basicAuthHeader, body);

        if (response === null) {
            console.warn('Create new authorization aborted.');
            return null;
        }

        if (response.ok === false) {
            console.error('Failed to create new authorization.', response);
            return null;
        }

        return (await response.json()).token as string;
    }

    private findAuthorization(authorizations: IAuthorization[]): IAuthorization | null {
        for (const authorization of authorizations) {
            if (authorization.app && authorization.app.name === GitHubVaultStorage.AUTHORIZATION_NAME) {
                return authorization;
            }
        }

        return null;
    }

    private async getToken(): Promise<string | null> {
        const storedToken: string | null = await this.keyValueStorage.getValue(GitHubVaultStorage.KEY_VALUE_STORAGE_TOKEN_KEY_NAME);

        if (storedToken !== null) {
            return storedToken;
        }

        const authorizations: IAuthorization[] | null = await this.listAuthorizations();

        if (authorizations === null) {
            return null;
        }

        const authorization: IAuthorization | null = this.findAuthorization(authorizations);

        if (authorization !== null) {
            if (await this.deleteAuthorization(authorization) === false) {
                return null;
            }
        }

        const token: string | null = await this.createAuthorization();

        if (token === null) {
            return null;
        }

        await this.keyValueStorage.setValue(GitHubVaultStorage.KEY_VALUE_STORAGE_TOKEN_KEY_NAME, token);

        return token;
    }

    private async ensureToken(): Promise<boolean> {
        if (this.token === null) {
            this.token = await this.getToken();
        }

        return this.token !== null;
    }

    private constructVaultFileUrl() {
        return `/repos/${this.repositoryOwner}/${this.repositoryName}/contents/${this.vaultFilename}`;
    }

    public async getVaultContent(): Promise<string | null> {
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
                this.keyValueStorage.removeValue(GitHubVaultStorage.KEY_VALUE_STORAGE_TOKEN_KEY_NAME);
                this.token = null;
                this.oneTimePassword = null;
                return await this.getVaultContent();
            }

            console.error(`Failed to fetch vault file '${this.vaultFilename}'.`, response);

            return null;
        }

        const responseContent: IGitHubContent = await response.json();

        this.currentVaultContentHash = responseContent.sha;

        return atob(responseContent.content);
    }

    async setVaultContent(newContent: string, updateMessage: string): Promise<boolean> {
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
    };
}
