# Design decisions

## Why ItchyPassword does not support multiple vaults or backup connectors

### Summary

ItchyPassword uses a **single active connector** model: only one storage backend (GitHub, Google Drive, etc.) is active at a time. There is no built-in backup connector, no multi-vault synchronization, and no automatic replication between connectors.

This is a deliberate decision, not an oversight. The multi-connector system has been built, shipped, then removed after evaluating the complexity cost. This document explains why.

---

### What has been built

The original connector architecture supported three **roles** per connector:

| Role | Behavior |
|------|----------|
| **Main** | The primary vault — read and write. Only one connector could be Main. |
| **Backup** | Write-only mirror. Multiple connectors could be Backup. Every save was replicated in parallel to all Backup connectors. |
| **Disabled** | Connector not used. |

Each connector also reported **granular access** via a `ConnectorAccessResult` struct:

```csharp
public struct ConnectorAccessResult
{
    public required bool CanRead { get; init; }
    public required bool CanWrite { get; init; }
}
```

The Settings page (~650 lines of Razor) let users assign roles via a rich dropdown with contextual hints (e.g., "No write access — cannot be Backup"), handled role transitions (Main→Backup was blocked, Disabled→Backup required a vault comparison), and showed multiple confirmation modals depending on the state.

When promoting a Disabled connector to Main, the system would:

1. Call `AccessAsync` to verify read+write permissions.
2. Download the remote vault from the new connector.
3. Compare vault signatures (HMAC-based) to detect conflicts.
4. Depending on the comparison result (`Identical`, `RemoteEmpty`, `LocalEmpty`, `Different`), either auto-merge, prompt the user with a conflict resolution modal, or silently push the current vault.

A similar flow existed for enabling a Backup connector, with an additional "write-only connector" modal path for connectors that couldn't read their own data.

The `SaveVaultAsync` method returned an array of per-connector results:

```csharp
Task<(IVaultConnector Connector, bool Success, string Error)[]> SaveVaultAsync(...)
```

Every caller (NewItemView, EditItemView) had to handle partial failures — some connectors succeeding, others failing — with logic like "if ALL failed → rollback; if SOME failed → stay on page but don't rollback."

### The complexity cost

Each design axis multiplied the state space:

- **3 roles** × **4 access combinations** (read/write/both/none) × **4 comparison states** (identical/remote-empty/local-empty/different) = dozens of UI paths.
- The Settings page alone had **4 different modals**: Main conflict, Backup overwrite, Write-only backup, and a generic confirmation.
- Partial save failure handling added branching in every page that saved the vault.
- Role validation on configuration change (e.g., changing a connector's access mode could auto-downgrade its role) added yet another layer.

The code worked, but:

1. **Testing was manual-only.** No automated tests covered the role transition flows because they required live connector interactions (GitHub API, Google Drive OAuth). Each change risked regressions in edge cases.
2. **User confusion.** The Main/Backup/Disabled dropdown required understanding vault synchronization concepts. Most users just want to store their vault in one place.
3. **False sense of security.** Backup connectors replicated on save, not on load. If the app crashed between saves, or if a Backup connector's write failed silently, the backup could be stale. Real backup requires versioning and conflict resolution, not fire-and-forget replication.
4. **Vault comparison was nearly useless.** In practice, vaults on different connectors almost always differ (different edit histories). The "Identical" shortcut path almost never triggered, making the comparison logic dead weight.

### What replaced it

The simplified model:

- **One active connector.** `VaultSession` stores a single `_activeConnectorId`. No roles, no backup set.
- **`AccessAsync` returns `bool`.** Either the connector has full access or it doesn't. No granular read/write distinction.
- **`SaveVaultAsync` returns `(bool, string)`.** Single result — success or failure with an error message.
- **Switching connectors always warns.** No vault comparison. A simple modal says "data may differ, unsaved changes will be lost." The user confirms or cancels.
- **Settings page is ~220 lines** (down from ~650). Each connector card shows an "Active" badge or an "Activate" button. No dropdown, no role management.

### Files removed

| File | Purpose |
|------|---------|
| `ConnectorRole.cs` | `enum ConnectorRole { Main, Backup, Disabled }` |
| `ConnectorAccessResult.cs` | `struct { CanRead, CanWrite }` |
| `VaultComparisonResult.cs` | `struct { Status, LocalItemCount, RemoteItemCount }` + `VaultComparisonStatus` enum |
| `LocalFileVaultConnector.cs` | Browser File System Access API connector (removed independently) |
| `localFile.js` | JS interop for the local file connector |

### The lesson

Each configurability axis has a multiplicative cost on UI states, edge cases, and test surface. A feature that sounds simple ("let users also write to a backup") cascades into role validation, conflict detection, partial failure handling, and multi-modal confirmation flows.

For a privacy-first tool where the vault is a single encrypted blob, the right answer is: **one connector, one vault, total simplicity.** Users who want redundancy can export their vault file manually or configure their storage backend's own backup features (GitHub repo mirroring, Google Drive backup, etc.).
