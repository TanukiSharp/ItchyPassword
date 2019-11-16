import { IStorage } from './IStorage';
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

export class GitHubStorage implements IStorage {
    static BASE_URL: string = 'https://api.github.com';
    static AUTHORIZATION_NAME: string = 'ItchyPassword_aae9385adee54b0c8c16c077f3cee256';

    private repositoryOwner: string;
    private basicAuthHeader: string;

    private token: string | null = null;
    private oneTimePassword: string | null = null;
    private currentVaultContentHash: string | null = null;

    public constructor(username: string, password: string, private repositoryName: string, private vaultFilename: string) {
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
        return `${GitHubStorage.BASE_URL}${relativeUrl}`;
    }

    private async request(method: string, relativeUrl: string, authHeader: string, body: any = undefined): Promise<Response | null> {
        const url: string = this.constructUrl(relativeUrl);
        const requestInfo: RequestInit = this.constructFetchRequest(method, authHeader, body);

        let response: Response = await fetch(url, requestInfo);

        if (response.status === 401) {
            this.oneTimePassword = prompt('Input your 2FA code:');

            if (this.oneTimePassword === null) {
                return null;
            }

            return await this.request(method, relativeUrl, authHeader, body);
        }

        return response;
    }

    private async listAuthorizations(): Promise<IAuthorization[] | null> {
        const response: Response | null = await this.request('GET', '/authorizations', this.basicAuthHeader);

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
        const response: Response | null = await this.request('DELETE', `/authorizations/${authorization.id}`, this.basicAuthHeader);

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
            note: GitHubStorage.AUTHORIZATION_NAME
        };

        const response: Response | null = await this.request('POST', '/authorizations', this.basicAuthHeader, body);

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
            if (authorization.app && authorization.app.name === GitHubStorage.AUTHORIZATION_NAME) {
                return authorization;
            }
        }

        return null;
    }

    private async getToken(): Promise<string | null> {
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

        return await this.createAuthorization();
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
        const response: Response | null = await this.request('GET', url, this.constructTokenAuthString());

        if (response === null) {
            console.warn('Fetching vault content aborted.');
            return null;
        }

        if (response.ok === false) {
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
        const response: Response | null = await this.request('PUT', url, this.constructTokenAuthString(), body);

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
