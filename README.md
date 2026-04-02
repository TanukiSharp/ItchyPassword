# ItchyPassword

This project is still under development, getting stable but will probably keep changing during the course of its evolution.

![](nothing-to-see-here.gif)

[ItchyPassword] is a privacy-first, offline-capable password manager built with **Blazor WebAssembly**. All cryptography happens in the browser via the [SubtleCrypto] API. No server, no backend, no telemetry.

Works on Chromium-based browsers (Edge, Chrome) and Firefox, on both desktop (Windows, Linux, macOS) and mobile (Android).

Vault storage connectors can optionally sync your encrypted data to external services (GitHub, Google Drive, SOLID Pod), but the app is fully functional offline. Those connectors only ever see encrypted blobs.

**Your master key never leaves the machine and is never stored anywhere** — not even encrypted. It lives in memory only while the tab is open and must be re-entered each time you start the app or refresh the page. This is by design.

> **Note**
> The Blazor rewrite (v2) is still in beta-testing and available at:
> https://tanukisharp.github.io/ItchyPassword/vnext
>
> The original TypeScript version remains at:
> https://tanukisharp.github.io/ItchyPassword

## Build

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later) installed.

```sh
dotnet publish -c Release -p:RunAOTCompilation=true
```

The published output will be in `docs/vnext`.

### AOT compilation

The `-p:RunAOTCompilation=true` argument is **optional**. Here are the trade-offs:

| | With AOT (`true`) | Without AOT (default) |
|---|---|---|
| **Runtime performance** | Near-native speed for C# logic (UI, JSON parsing, search, encoding) | Interpreted, noticeably slower general app responsiveness |
| **App download size** | Larger (several MB more) | Smaller initial download |
| **Build time** | Much slower (minutes) | Fast (seconds) |
| **Best for** | Production / daily use | Quick development iterations |

> **Note**
> The heavy cryptographic operations (PBKDF2 key derivation, AES-GCM encryption, HMAC-SHA-512) all run in the browser's native SubtleCrypto API via JavaScript interop, so they are **not** affected by AOT. AOT improves everything else: UI rendering, vault deserialization, search, encoding, and general app responsiveness.

For development, you can skip AOT:

```sh
dotnet watch run --project src/ItchyPassword.Client/ItchyPassword.Client.csproj
```

> **Tip**
> If the build fails with file lock errors, run `dotnet clean` first or kill lingering `dotnet` processes.

## Features overview

### Master key

The master key is the only thing you need to remember. It is entered at launch and held in memory for the duration of your session.

A confirmation field lets you double-check your input: it turns green when both fields match, red when they don't. This is important when you create your very first items offline — you want to make sure you didn't mistype.

If you don't submit your master key within ~30 seconds of inactivity (no typing), it is automatically cleared from the input field for security reasons. Once submitted, the key remains in memory until the page is closed, reloaded, or discarded by the browser (which can happen on mobile).

![](./Documentation/screenshots/01_master_key.png)

> **Note**
> Your master key should be long and unpredictable.
> It is recommended to use Diceware™ to generate it.
>
> You can find a web-based implementation at https://tanukisharp.github.io/Diceware/ ([details](https://github.com/TanukiSharp/Diceware))

### Vault

The vault is a simple JSON file that stores all your items (static keys, secrets). It can be easily archived, copied, or backed up like any other file. Once your master key is entered and a connector is configured, the vault is fetched and loaded — but **items are not all decrypted at once**. Each item is decrypted on-demand as you navigate to it, keeping exposure to a minimum.

![](./Documentation/screenshots/02_vault.png)

From the vault view, you can:
- **Search** items by name or metadata, with three search modes: Contains, Fuzzy, and Exact, with match highlighting.
- **Filter** items by type (all, static keys only, secrets only).
- **Copy** a static key or decrypted secret to the clipboard with a single click.
- **Edit** any item by clicking on it. You can delete item from the edit view.
- **Create** new items via the `+` button.
- **Reload** the vault from the configured connector.

Item names are split into actionable segments: parts that look like URLs become clickable links, and other parts can be copied individually with a click.

### Static key (formerly "Password")

A static key is a **deterministic** value derived from your master key and a public part. Given the same inputs, the same output is always generated. This means nothing needs to be stored to regenerate it — you only need to remember your master key and the public part. This is particularly useful for services where the vault itself is stored (e.g., GitHub, Google), since you can regenerate the password without having access to the vault, avoiding a lock-out situation.

Only the public part, alphabet, length, and version are stored in the vault — all unencrypted, and none of it is sensitive. The generated key itself is never stored anywhere.

![](./Documentation/screenshots/03_static_key.png)

Options include:
- **Public part**: a memorable string that, combined with your master key, produces the key. For example, `github.com/myemail@example.com`.
- **Alphabet**: the set of characters allowed in the generated output.
- **Length**: how many characters the output should be.
- **Version**: the derivation version (v1 = 100,000 PBKDF2 iterations, v2 = 400,000).

The output supports two display modes:
- **Plaintext**: view and copy the generated key.
- **QR code**: renders the generated key as a QR code.

> **Warning**
> For services you need to access to bootstrap your vault (e.g., GitHub), the public part should be something you know and remember by heart.
> If you generate a random public part and store it only in your vault, you could lock yourself out.
>
> Also, if you customized the alphabet, length, or version, you will need to remember those parameters to regenerate the exact same key without the vault handy. For simplicity, it is recommended to keep the default values so you only need to remember the public part.

### Secret (formerly "Cipher")

A secret is a **free-form text**, stored encrypted with your master key. Unlike static keys, secrets are not deterministic value once encrypted — they are actual ciphertext in the vault.

![](./Documentation/screenshots/04_secret.png)

Secrets support three display modes:
- **Plaintext**: view and edit the decrypted content.
- **TOTP**: if the secret contains a TOTP seed (base32-encoded), it displays a live 6-digit code with a 30-second countdown timer, with copy support.
- **QR code**: renders the decrypted content as a QR code.

A built-in **secret generator** can produce random secrets with configurable rules:
- Total length
- Minimum count of lowercase, uppercase, digits, and symbols
- Custom symbol alphabet

### Metadata

Every vault item (static key or secret) can have **key-value metadata** attached to it. Each metadata entry can optionally be **encrypted** independently — useful for storing sensitive notes like usernames, recovery codes, or account IDs alongside an item without them being visible in the vault file.

> **Note**
> When encrypting metadata, only the value is encrypted, not the key. This lets you search for items by metadata key even when the values are protected.

![](./Documentation/screenshots/05_metadata.png)

### Empty page

A blank page accessible from the navigation sidebar. Its only purpose is to quickly hide the screen content if you have people around.

### Error log

A debug page that collects runtime errors in memory. Useful for diagnosing issues on mobile browsers where the developer console is not easily accessible.

### Raw vault

Another debug page that displays the encrypted vault content as-is (the raw JSON blob), useful for diagnostics.

### Theme support

The app supports **System**, **Light**, and **Dark** themes. The preference is persisted in local storage.

## Getting started

If you're new to ItchyPassword and not a power user, the easiest way to get going is with the **Google Drive** connector. It requires no developer setup — just a Google account. The GitHub connector requires creating a repository and a personal access token, and SOLID pods are still niche. For most people, Google Drive is the simplest choice.

Because your vault is stored on Google Drive, you need to be able to log in to Google **without** having access to the vault. It is strongly recommended to change your Google password to a **Static key** — a deterministic value you can always regenerate from your master key and a public part — especially if your current password is not strong enough. This way, you avoid a lock-out situation where you can't access the vault because you can't log in to Google.

### New users

1. Open [ItchyPassword] and enter your master key.
2. You'll be redirected to the **Settings** page to configure a connector. Skip this for now.
3. Navigate to the **Static key** page from the sidebar. This page lets you generate a static key without creating a vault item yet.
4. Set the **public part** to something memorable, for example `google.com/myemail@gmail.com`.
5. Leave alphabet, length, and version at their defaults so you can regenerate this key from memory alone.
6. Copy the generated static key and go to your Google account settings to change your password to it.
7. Go back to [ItchyPassword], navigate to **Settings**, and configure the **Google Drive** connector. For more information about setting up the **Google Drive** connector, refer to the **Google Drive** section below. Once done, click the **Activate** button.
8. A Google authentication popup will open — log in with your (new) Google password.
9. Navigate to the **Vault** page and click the `+` button to create a new item.
10. Choose **Static key** as the type and set the same public part you used in step 4 (e.g., `google.com/myemail@gmail.com`). Keep the default alphabet, length, and version.
11. Save the item. You're all set.

> **Warning**
> Make sure you remember the public part you chose for Google. If you forget it, you won't be able to regenerate the static key, and you'll be locked out of both Google and your vault.

### Existing users migrating to Google Drive

If you already use ItchyPassword with another connector (e.g., GitHub) and want to switch to Google Drive:

#### 1. Load your existing vault

The previous version of ItchyPassword only supported the GitHub connector, so your vault is likely stored there.

1. Open [ItchyPassword] and enter your master key.
2. Go to **Settings** and configure the **GitHub** connector with your repository details and personal access token (PAT).
3. Load your vault from GitHub to make sure all your items are available.

#### 2. Ensure your Google password is a Static key

4. Check how your Google password is currently stored in the vault. If it's already a **Static key**, skip to step 8.
5. If your Google password is stored as a **Secret**, you need to replace it. The type of a vault item cannot be changed, so you'll need to delete the existing Secret and create a new Static key in its place.
6. Create a new **Static key** item with a memorable public part (e.g., `google.com/myemail@gmail.com`). Keep the default alphabet, length, and version.
7. Copy the generated static key and change your Google account password to it. Then delete the old Secret item. Save your vault to GitHub so the changes are persisted.

#### 3. Transfer your vault to Google Drive

8. Go to **Settings** and activate the **Google Drive** connector. A Google authentication popup will open — log in with your Google password.
9. Click **Export** to Google Drive to transfer all your existing vault content.

You can keep the GitHub connector configured alongside Google Drive, or clear the configuration once you've confirmed everything works.

## Vault connectors

Vault connectors are storage backends that handle fetching and saving the encrypted vault blob. You can configure them in the **Settings** page. The app supports multiple connectors, and you choose which one to use for reading and writing.

### GitHub

Stores your vault as a JSON file in a GitHub repository. Changes are committed, so your data is versioned and can be reverted.

You need:
- A GitHub account
- A repository (e.g., `ItchyPasswordVault`)
- A personal access token (PAT) with `Contents: Read and write` permission on that repository

The PAT is encrypted with your master key before being stored in local storage.

> **Note**
> Fine-grained tokens scoped to a single repository are recommended over classic tokens.

### Google Drive

Stores your vault as a file in Google Drive. Authentication uses OAuth 2.0 with PKCE (no client secret stored in the app).

Two storage modes are available:

> **Warning**
> Please read the pros and cons of each mode carefully before choosing. The implications — especially around data retention when revoking access and manual backup capabilities — are significant and changing later is possible but not user friendly.

#### App data folder (default)

The vault file is stored in a hidden, app-specific area of your Google Drive (`appDataFolder`). This space is managed entirely by ItchyPassword and is **not visible** in the Google Drive UI — you won't see it when browsing your files.

**Pros:**
- Keeps your Google Drive clean — no extra folders or files cluttering your drive.
- The vault cannot be accidentally renamed, moved, or deleted by the user.
- Uses the most restrictive OAuth scope (`drive.appdata`), which only grants access to the app's own hidden folder — ItchyPassword cannot see or touch any of your other Drive files.
- No other application can access the vault file — the app data folder is isolated per application.

**Cons:**
- You cannot manually browse, download, or back up the vault file from Google Drive.
- If you uninstall or revoke ItchyPassword's access from your Google account, the hidden app data is deleted by Google — your vault will be lost unless you have a backup elsewhere.
- Debugging or manual recovery is harder since the file is not directly accessible.

#### User folder

The vault file is stored in a regular, user-visible folder in your Google Drive.

**Pros:**
- The vault file is visible in your Google Drive — you can browse, download, or back up the file manually at any time.
- Revoking the app's access does **not** delete the file — it stays in your Drive like any other file.
- Easier to migrate or share across devices since you can see exactly where the file is.

**Cons:**
- The file can be accidentally renamed, moved, or deleted by the user, which would break the connector.
- Uses a broader OAuth scope (`drive.file`), which grants ItchyPassword access to files it creates or that the user opens with it. This is still reasonably scoped, but less restrictive than the app data mode.
- Adds a visible folder to your Google Drive.

#### Folder path (User folder mode only)

When using User folder mode, you need to specify a **folder path** (not a file path). For example, `ItchyPassword` or `ItchyPassword/Vaults`. The folder is created automatically if it does not exist.

Inside this folder, two files are created and managed by the app:
- `vault.json` — the encrypted vault blob.
- `history.txt` — a log of vault operations (save timestamps, etc.).

Do not rename or move these files manually.

### SOLID Pod

Stores your vault in a [SOLID](https://solidproject.org/) pod. Authentication uses SOLID's OpenID Connect flow with DPoP token binding.

## How it works

### Static key generation

Input elements:
1. Your master key (string → UTF-8 bytes)
2. A public part (string → UTF-8 bytes)
3. A purpose value (string → UTF-8 bytes)

A derived key is computed using [PBKDF2] from `1`, with `2` as salt, using [SHA-512] as the hash algorithm.
Then `3` is hashed using [HMAC]-[SHA-512] with 256 bits from the derived key as the secret.
The output is encoded using a configurable alphabet.

| Version | PBKDF2 iterations |
|---|---|
| v1 | 100,000 |
| v2 | 400,000 |

### Secret encryption

Secrets are encrypted using AES-GCM via the browser's SubtleCrypto API, with a key derived from your master key. The encrypted blob is stored in the vault alongside a version tag for future-proof decryption.

### Vault integrity

The vault file includes an HMAC signature computed over the canonical JSON content. On load, the signature is verified to detect tampering or corruption.

A migration service handles upgrading vaults from older formats.

### Generate a personal access token (on GitHub)

This section only applies if you use the **GitHub** vault connector.

> The procedure below is accurate as of 2026, but may change on GitHub's side.

1. Open https://github.com and log in.<br/>
    If you are setting up things on a new device:

    a. Open [ItchyPassword].

    b. Enter your master key.

    c. Enter your predictable public part for GitHub (e.g., `github.com/<your-email-address>`) in the Static key section.

    d. Log in to GitHub with the generated static key.

2. Go to `Settings` → `Developer settings` → `Personal access tokens`.
3. Choose `Fine-grained tokens` (recommended) or `Tokens (classic)`.
4. Click `Generate new token`.
5. Name the token. Recommended naming is: `ItchyPassword (<user> / <device> / <browser>)`.

    Examples:
    - `ItchyPassword (Alice / Desktop / Edge)`
    - `ItchyPassword (Bob / Pixel 6a / Chrome)`

    This helps identify which token to revoke if a device is lost.

6. Set `Expiration` to 90 days or less.
7. Configure permissions:

    **Fine-grained tokens**:
    - `Repository access` → `Only select repositories` → select your vault repository.
    - `Permissions` → `Repository permissions` → `Contents` → `Read and write`.

    **Classic tokens**:
    - Check the `repo` scope.

8. Click `Generate token` and copy the result into the GitHub connector settings in [ItchyPassword].

### Set up a SOLID pod

This section only applies if you use the **SOLID Pod** vault connector.

You need a SOLID pod from any Solid-OIDC compatible provider (e.g., [Inrupt](https://inrupt.com/)). The connector requires two URLs that are typically on **different subdomains**, which can be confusing at first:

- **Provider URL** (Issuer): the OIDC identity provider, used for authentication. For example, `https://login.inrupt.com`.
- **Vault file URL**: the full URL of the vault file in your pod storage. For example, `https://storage.inrupt.com/<your-pod-id>/ItchyPassword/vault.json`.

These are different servers — one handles identity, the other handles data storage. You can usually find your storage URL by logging into your pod provider's dashboard and looking at your pod's root URL, then appending a path of your choice (e.g., `ItchyPassword/vault.json`).

An optional **Client ID** field is available. Most providers support dynamic client registration, so you can leave it empty and let ItchyPassword register automatically. Fill it in only if your provider requires a pre-registered client.

> **Note**
> The DPoP key pair used for token binding is ephemeral — generated fresh in the browser each session and never stored anywhere.

## Legal

- [Privacy Policy](https://tanukisharp.github.io/ItchyPassword/privacy-policy.html)
- [Terms of Service](https://tanukisharp.github.io/ItchyPassword/terms-of-service.html)



[HMAC]: https://en.wikipedia.org/wiki/HMAC
[SHA-512]: https://en.wikipedia.org/wiki/SHA-2
[PBKDF2]: https://en.wikipedia.org/wiki/PBKDF2
[SubtleCrypto]: https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto
[ItchyPassword]: https://tanukisharp.github.io/ItchyPassword/vnext
