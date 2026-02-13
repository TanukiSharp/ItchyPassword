## Plan: ItchyPassword Modernization (Bleeding Edge)

Rewrite "ItchyPassword" as a standalone, offline-first **C# / .NET 10 Blazor WebAssembly PWA**. This plan prioritizes data portability (Git), deterministic storage (clean diffs), and a "pure client" architecture with a hybrid C#/JS crypto stack.

**Steps**

1.  **Project Scaffolding (.NET 10)**
    *   **Solution**: `ItchyPassword.sln`
    *   **`ItchyPassword.Core`**: Class Library (.NET 10).
        *   Contains strictly platform-agnostic code: Domain Models (`VaultEntry`), Interfaces (`IVaultStorage`, `ICryptoService`), and ViewModels.
    *   **`ItchyPassword.Client`**: Blazor WebAssembly Project.
        *   **PWA**: Enabled for offline access (Service Worker).
        *   **Dependencies**: `Microsoft.AspNetCore.Components.WebAssembly.DevServer` (for dev), `System.Text.Json` (core).
        *   **Static Assets**: JS modules in `wwwroot/js/` (no bundlers).

2.  **Domain & Data Model (`ItchyPassword.Core`)**
    *   **`VaultEntry`**:
        *   `Id` (Guid), `Folder` (string, path-based), `Name` (string), `LastModified` (DateTime).
        *   `IsDeterministic` (bool): Defines the "Type" of entry.
        *   `Content`: A wrapper/polymorphic storage.
            *   *Scenario A (Deterministic)*: Stores `{ PublicPart, Length, Alphabet, Version }`.
            *   *Scenario B (Cipher)*: Stores `{ CipherText, IV, Version }`.
        *   `CustomFields`: `List<CustomField>` where `CustomField` has `{ Key, Value, IsEncrypted }`.
        *   *Note*: `GeneratorConfig` (Min Upper, Min Lower, etc.) is **NOT** stored. It is purely transient UI state when generating a new random password.
    *   **Serialization Logic**:
        *   Implement a custom `JsonConverter` or `ContractResolver` in the Storage Service.
        *   **Rule**: Recursively **sort all object properties alphabetically** and sort `VaultEntry` lists by ID/Name before serialization. This guarantees clean Git diffs.

3.  **Cryptographic Architecture (Hybrid)**
    *   **JS Layer (`wwwroot/js/`)**:
        *   `crypto.js`: Wrapper for `window.crypto.subtle` (AES-GCM, PBKDF2).
        *   `encoding.js`: Existing Base58/Base62 logic (ported to ES6 module).
        *   `random.js`: Exposes `getRandomValues`.
    *   **C# Layer (`ItchyPassword.Client`)**:
        *   `CryptoService`: Handles JS Interop calls.
        *   `PasswordGenerator`: Pure C# logic. Calculates *character indices* using random bytes fetched from JS.

4.  **Vault Storage & Authentication**
    *   **`GitHubDeviceAuthService`**:
        *   Implements Device Flow (User enters code on GitHub website).
        *   Persists Access Token in `LocalStorage` (AES-Encrypted with Master Key which is **memory-only**).
    *   **`GitHubVaultStorage`**:
        *   Uses `HttpClient` to fetch/push `vault.json`.
        *   Handles 401: Redirects to Auth.
        *   **Auto-Migration**: Detects legacy format -> Flattens to `VaultEntry` list (Folder = Path) -> Saves immediately.

5.  **User Interface (Bootstrap 5)**
    *   **Login / Offline Landing**:
        *   Input: Master Key.
        *   **"Standalone Generator"**: A critical feature for the "Deadlock" scenario. Opens a modal to input `Public Part` + `Master Key` (from memory) to generate a password immediately without network/vault.
    *   **Vault Editor**:
        *   **Cipher Mode (Default)**:
            *   "Generate" button opens modal -> User sets rules (Length, Min U/L/N/S) -> Generates Random String -> Fills "Secret" field.
            *   Only the resulting *Secret* is stored (encrypted).
        *   **Deterministic Mode (Checkbox)**:
            *   Hides "Secret" field. Shows "Public Part", "Length", "Alphabet".
        *   **Custom Fields**: Data grid with "Lock" toggle per row.

6.  **Build & Operations**
    *   **CI/CD**: standard `dotnet publish` to `docs` folder.
    *   **Versioning**: `VaultEntry` has a `Version` property to handle future schema changes safely.

**Verification**
*   **Git Diffs**: Create/Edit vault -> Save -> Verify JSON is sorted.
*   **Offline Access**: Disconnect network -> Open App (PWA) -> Use "Standalone Generator" -> Verify correctness.
*   **Legacy Compat**: Ensure generated passwords match the old TS app outputs exactly.

**Decisions**
*   **No Persisted Config**: As requested, we do not store "Min Upper/Lower" rules. Users re-enter them if they regenerate.
*   **Length in Vault**: Explicitly stored for Deterministic entries (mandatory) and effectively the length of the decrypted string for Ciphers.
*   **Master Key**: Never stored on disk/localstorage. Only the *Encrypted Auth Token* is stored.
*   **Standalone Mode**: Critical path feature added to ensure users can log in to GitHub *to* get their vault access token.
