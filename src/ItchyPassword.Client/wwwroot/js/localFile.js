/**
 * JS interop for the Local File vault connector.
 * Uses the File System Access API to read/write a local vault file.
 * The file handle is persisted in IndexedDB so it survives page reloads.
 * Only supported in Chromium-based browsers (Chrome, Edge, Opera).
 *
 * Two-phase access flow (to preserve browser user gesture):
 * 1. initiateAccess() — called synchronously from C# (IJSInProcessRuntime) to start the picker/permission.
 * 2. awaitAccess()    — called from C# AccessAsync, awaits the result.
 */
window.localFileInterop = {
    /** @type {FileSystemFileHandle|null} */
    _handle: null,

    /** @type {boolean} Whether _handle was restored from IndexedDB and still needs permission. */
    _needsPermission: false,

    /** @type {Promise<string|null>|null} Pending access result set by initiateAccess(). */
    _accessPromise: null,

    _dbName: 'itchypassword_localfile',
    _storeName: 'fileHandles',
    _key: 'vaultFileHandle',

    /** @returns {Promise<IDBDatabase>} */
    _openDb() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this._dbName, 1);
            request.onupgradeneeded = () => request.result.createObjectStore(this._storeName);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Stores the given handle in IndexedDB for cross-session persistence.
     * @param {FileSystemFileHandle} handle
     * @returns {Promise<void>}
     */
    async _persistHandle(handle) {
        const db = await this._openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(this._storeName, 'readwrite');
            const store = tx.objectStore(this._storeName);
            const request = store.put(handle, this._key);
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Checks whether a handle is stored in IndexedDB.
     * @returns {Promise<boolean>} True if a handle exists.
     */
    async hasStoredHandle() {
        try {
            const db = await this._openDb();
            const handle = await new Promise((resolve, reject) => {
                const tx = db.transaction(this._storeName, 'readonly');
                const store = tx.objectStore(this._storeName);
                const request = store.get(this._key);
                request.onsuccess = () => resolve(request.result || null);
                request.onerror = () => reject(request.error);
            });

            return handle !== null;
        } catch {
            return false;
        }
    },

    /**
     * Restores the file handle from IndexedDB into memory.
     * Called during initialization (LoadConfigurationAsync) — no user gesture needed.
     *
     * Uses queryPermission() to check if the user previously granted persistent access
     * ("allow every time"). If so, _needsPermission is set to false and the subsequent
     * initiateAccess() call will resolve instantly without any prompt.
     *
     * @returns {Promise<boolean>} True if a handle was restored.
     */
    async restoreHandle() {
        try {
            const db = await this._openDb();
            const handle = await new Promise((resolve, reject) => {
                const tx = db.transaction(this._storeName, 'readonly');
                const store = tx.objectStore(this._storeName);
                const request = store.get(this._key);
                request.onsuccess = () => resolve(request.result || null);
                request.onerror = () => reject(request.error);
            });

            if (handle) {
                this._handle = handle;

                // queryPermission() does NOT require a user gesture and does NOT show a prompt.
                // If the user previously chose "allow every time", it returns 'granted'.
                const permission = await handle.queryPermission({ mode: 'readwrite' });
                this._needsPermission = permission !== 'granted';

                return true;
            }
        } catch {
            // Ignore IndexedDB errors.
        }

        return false;
    },

    /**
     * Initiates the access flow.
     * Called synchronously from C# (IJSInProcessRuntime) to preserve the browser's
     * transient user activation for showOpenFilePicker / requestPermission.
     *
     * If the user previously chose "allow every time", queryPermission() in restoreHandle()
     * already set _needsPermission to false, so this resolves instantly.
     *
     * Sets _accessPromise which is consumed by awaitAccess().
     */
    initiateAccess() {
        if (typeof window.showOpenFilePicker !== 'function') {
            this._accessPromise = Promise.resolve(null);
            return;
        }

        // Already accessed this session — resolve immediately.
        if (this._handle && !this._needsPermission) {
            this._accessPromise = Promise.resolve(this._handle.name);
            return;
        }

        // Handle restored from IndexedDB but permission not yet granted — request it (needs gesture).
        if (this._handle && this._needsPermission) {
            this._accessPromise = this._handle.requestPermission({ mode: 'readwrite' })
                .then(result => {
                    if (result === 'granted') {
                        this._needsPermission = false;
                        return this._handle.name;
                    }
                    // Permission denied — clear handle so next click opens picker.
                    this._handle = null;
                    this._needsPermission = false;
                    return null;
                })
                .catch(() => {
                    this._handle = null;
                    this._needsPermission = false;
                    return null;
                });
            return;
        }

        // No handle — open the file picker (needs gesture).
        this._accessPromise = window.showOpenFilePicker({
            types: [{ description: 'JSON files', accept: { 'application/json': ['.json'] } }],
            multiple: false
        }).then(([handle]) => {
            this._handle = handle;
            this._needsPermission = false;
            return this._persistHandle(handle).then(() => handle.name);
        }).catch(() => null);
    },

    /**
     * Awaits the result of a previously initiated access.
     * Called from C# AccessAsync after initiateAccess() was triggered.
     * @returns {Promise<string|null>} The file name, or null if access failed.
     */
    async awaitAccess() {
        if (this._accessPromise === null) {
            return null;
        }

        const result = await this._accessPromise;
        this._accessPromise = null;
        return result;
    },

    /**
     * Reads the content of the selected file.
     * @returns {Promise<string|null>} The file content, or null if no handle.
     */
    async readFile() {
        if (this._handle === null) {
            return null;
        }
        const file = await this._handle.getFile();
        return await file.text();
    },

    /**
     * Writes content to the selected file.
     * @param {string} content - The content to write.
     * @returns {Promise<boolean>} True if the write succeeded.
     */
    async writeFile(content) {
        if (this._handle === null) {
            return false;
        }
        const writable = await this._handle.createWritable();
        await writable.write(content);
        await writable.close();
        return true;
    }
};

// Global wrapper for synchronous invocation from Blazor IJSInProcessRuntime.
// Avoids eval() so a strict Content-Security-Policy can omit 'unsafe-eval'.
window.localFileInitiateAccess = function () {
    localFileInterop.initiateAccess();
    return null;
};
