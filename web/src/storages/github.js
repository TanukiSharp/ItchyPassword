class GitHubStorage {
    static BASE_URL = 'https://api.github.com';
    static AUTHORIZATION_NAME = 'PasswordGenerator_aae9385adee54b0c8c16c077f3cee256';

    constructor(username, password, repositoryName, vaultFilename) {
        this._repositoryOwner = username;
        this._repositoryName = repositoryName;
        this._vaultFilename = vaultFilename;
        this._basicAuthHeader = this._constructBasicAuthString(username, password);
    }

    _constructBasicAuthString(username, password) {
        const authString = btoa(`${username}:${password}`);
        return `Basic ${authString}`;
    }

    _constructTokenAuthString() {
        return `token ${this._token}`;
    }

    _constructFetchRequest(method, authHeader, body) {
        const headers = {
            'Content-Type': 'application/json',
            'Authorization': authHeader
        };

        if (this._oneTimePassword) {
            headers['x-github-otp'] = this._oneTimePassword;
        }

        return {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined
        };
    }

    _constructUrl(relativeUrl) {
        return `${GitHubStorage.BASE_URL}${relativeUrl}`;
    }

    async _request(method, relativeUrl, authHeader, body) {
        const url = this._constructUrl(relativeUrl);
        const requestInfo = this._constructFetchRequest(method, authHeader, body);

        let response = await fetch(url, requestInfo);

        if (response.status === 401) {
            this._oneTimePassword = prompt('Input your 2FA code:');

            if (!this._oneTimePassword) {
                return null;
            }

            return await this._request(method, relativeUrl, authHeader, body);
        }

        return response;
    }

    async _listAuthorizations() {
        const response = await this._request('GET', '/authorizations', this._basicAuthHeader);

        if (!response) {
            console.warn('List authorizations aborted.');
            return null;
        }

        if (!response.ok) {
            console.error('Failed to list authorizations.', response);
            return null;
        }

        return await response.json();
    }

    async _deleteAuthorization(authorization) {
        const response = await this._request('DELETE', `/authorizations/${authorization.id}`, this._basicAuthHeader);

        if (!response) {
            console.warn('Delete authorization aborted.');
            return false;
        }

        if (!response.ok) {
            console.error(`Failed to delete authorization '${authorization.id}'.`, response);
        }

        return response.ok;
    }

    async _createAuthorization() {
        const body = {
            scopes: ['repo'],
            note: GitHubStorage.AUTHORIZATION_NAME
        };

        const response = await this._request('POST', '/authorizations', this._basicAuthHeader, body);

        if (!response) {
            console.warn('Create new authorization aborted.');
            return null;
        }

        if (!response.ok) {
            console.error('Failed to create new authorization.', response);
            return null;
        }

        return (await response.json()).token;
    }

    _findAuthorization(authorizations) {
        for (const authorization of authorizations) {
            if (authorization.app && authorization.app.name === GitHubStorage.AUTHORIZATION_NAME) {
                return authorization;
            }
        }

        return null;
    }

    async _getToken() {
        const authorizations = await this._listAuthorizations();

        if (authorizations === null) {
            return null;
        }

        const authorization = this._findAuthorization(authorizations);

        if (authorization !== null) {
            if (await this._deleteAuthorization(authorization) === false) {
                return null;
            }
        }

        return await this._createAuthorization();
    }

    async _ensureToken() {
        if (!this._token) {
            this._token = await this._getToken();
        }

        return Boolean(this._token);
    }

    _constructVaultFileUrl() {
        return `/repos/${this._repositoryOwner}/${this._repositoryName}/contents/${this._vaultFilename}`;
    }

    async getVaultContent() {
        if (await this._ensureToken() === false) {
            return null;
        }

        const url = this._constructVaultFileUrl();
        const response = await this._request('GET', url, this._constructTokenAuthString());

        if (!response) {
            console.warn('Fetching vault content aborted.');
            return null;
        }

        if (!response.ok) {
            console.error(`Failed to fetch vault file '${this._vaultFilename}'.`, response);
            return null;
        }

        const responseContent = await response.json();

        this._currentVaultContentHash = responseContent.sha;

        return atob(responseContent.content);
    }

    async setVaultContent(newContent, updateMessage) {
        if (await this._ensureToken() === false) {
            return null;
        }

        const body = {
            message: updateMessage,
            content: btoa(newContent),
            sha: this._currentVaultContentHash
        };

        const url = this._constructVaultFileUrl();
        const response = await this._request('PUT', url, this._constructTokenAuthString(), body);

        if (!response) {
            console.warn('Push new vault content aborted.');
            return null;
        }

        const responseContent = await response.json();

        if (!response.ok) {
            console.error(`Failed to create/update vault file '${this._vaultFilename}'.`, response, responseContent);
            return null;
        }

        this._currentVaultContentHash = responseContent.content.sha;

        return newContent;
    };
}
