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

Stores your vault as a file in Google Drive. Two modes are available:
- **App data folder** (hidden): the file lives in a special folder only accessible by the app.
- **User folder**: the file is stored in a regular, user-visible folder.

Authentication uses OAuth 2.0 with PKCE (no client secret stored in the app).

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
