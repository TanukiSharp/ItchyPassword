---
description: Comprehensive guide for AI coding agents working on ItchyPassword.
---

# ItchyPassword Copilot Instructions

## Project Overview
ItchyPassword is a privacy-first, offline-capable password manager built with **Blazor WebAssembly**.
- **Core Philosophy**: The Master Key is **never** stored (not even encrypted). It persists in memory (`IMasterKeyProvider`) only while the tab is open.
- **Architecture**:
  - `src/ItchyPassword.Client`: Main Blazor WASM application.
  - `src/ItchyPassword.Core`: Shared logic, models, and cryptography.

## Architectural Patterns

### State Management (`VaultSession` / `IMasterKeyProvider` / `UiState`)
- **VaultSession**: Manages the active `Vault`, the list of `VaultConnectors`, and connector preferences (reader/writer selection). Uses `INotifyPropertyChanged` to update UI components when state changes (e.g., loading/unloading via `Status`).
- **IMasterKeyProvider / MasterKeyProvider**: Owns the in-memory Master Key. Standalone service so that components needing only the key don't depend on the full session. `MasterKeyProvider` implements `INotifyPropertyChanged`.
- **UiState**: Lightweight property bag for cross-page UI state (e.g., `SearchQuery`) that survives navigation.
- **Persistence**: State itself is transient. Configuration is persisted via `LocalStorageService`, but secrets are not. If something secret is stored (token) it is encrypted with the user's master key.

### Vault Connectors (`IVaultConnector`)
- **Abstraction**: Storage backends implement `IVaultConnector` (e.g., `GitHubVaultConnector`, `GoogleDriveVaultConnector`, `LocalFileVaultConnector`, etc...).
- **Responsibility**: Connectors handle authentication, loading, and saving the encrypted vault blob. They do *not* handle decryption (that's `VaultService` / `ICryptoService`).
- **Configuration**: Each connector manages its own configuration serialization to `LocalStorage`.

### Cryptography
- **Service**: `ICryptoService` (in Core) abstracts encryption primitives.
- **Implementation**: Uses browser `SubtleCrypto` via JS interop.
- **Flow**: `VaultService` orchestrates the decryption of the vault blob using the key from `IMasterKeyProvider`.

## Developer Workflows

### Building & Running
- **Project**: Always target `src/ItchyPassword.Client`.
- **Commands**:
  ```bash
  dotnet build src/ItchyPassword.Client/ItchyPassword.Client.csproj
  dotnet watch run --project src/ItchyPassword.Client/ItchyPassword.Client.csproj
  ```
- **File Locking**: If build fails with file lock errors (~"process cannot access file"), run `dotnet clean` first or kill lingering `dotnet` processes.

### Navigation Flow
1. **`/` (Index)**: Checks if loaded. If not, shows `MasterKeyView`.
2. **Master Key**:
    - User inputs Master Key.
    - Key is stored in `IMasterKeyProvider`.
    - If no connectors configured -> Redirect to `/settings`.
    - If connectors configured -> Redirect to `/vault` on success, show error on failure.
3. **`/settings`**: Configure vault connectors.
4. **`/vault`**: User can see and search its vault.

## Coding Conventions

### Blazor Components
- **Dependency Injection**: Inject services (`VaultSession`, `IMasterKeyProvider`, `UiState`, `NavigationManager`, etc.) at the top of `.razor` files.
- **Async Interactions**: Prefer `async Task` for UI event handlers (e.g., `LoadAsync`).
- **Styles**: Use standard CSS or the project's custom variables (`var(--text-muted)`).
- **Notifications**: Updates to `VaultSession` or `MasterKeyProvider` should trigger UI refreshes automatically via binding or `StateHasChanged` if needed (though `INotifyPropertyChanged` logic in components is preferred if implemented).

### Security Rules
- **NO LOGGING**: Never log the Master Key or decrypted secrets to console or storage.
- **Memory Only**: Secrets exist only in `IMasterKeyProvider` and are wiped on refresh.

### File Structure
- `Components/`: Reusable UI parts (`MasterKeyView`, `VaultConnectorSettings`).
- `Pages/`: Routable views (`Settings`, `Vault`).
- `Services/`: Business logic (`VaultLoader`, `Connectors/`).
