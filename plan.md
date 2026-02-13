# Implementation Plan

## Goal
Rewrite ItchyPassword as a client-side Blazor WebAssembly application using C# 14 / .NET 10.

## Architecture
- **Framework**: Blazor WebAssembly (Standalone)
- **Language**: C# 14
- **Runtime**: .NET 10
- **Storage**: In-memory (sensitive data), LocalStorage (settings/tokens), GitHub (Vault)
- **Cryptography**: System.Security.Cryptography (or WebCrypto via JS Interop if needed, but .NET has good support now)

## Tasks
- [ ] Create Solution and Project Structure
    - [ ] Create `ItchyPassword.Blazor.sln` (or update existing)
    - [ ] Create `ItchyPassword.App` (Blazor WASM project)
    - [ ] Create `ItchyPassword.Core` (Shared logic, if needed)
- [ ] Implement Core Logic
    - [ ] Port `VaultEntry` and data models
    - [ ] Implement Crypto Service (PBKDF2, AES-GCM, etc.)
    - [ ] Implement Vault Storage (GitHub API client)
- [ ] Implement UI
    - [ ] Layout (MainLayout, NavMenu)
    - [ ] Master Key Entry / Unlock Screen
    - [ ] Password Generator Tab
    - [ ] Ciphers Tab
    - [ ] Vault Tab (Load/Save)
- [ ] Migration/Compatibility
    - [ ] Ensure compatibility with existing vault format (JSON)

## Notes
- Tooling is limited (no CLI access), so files will be created manually.
- Focusing on "Client-side only" requirement.
