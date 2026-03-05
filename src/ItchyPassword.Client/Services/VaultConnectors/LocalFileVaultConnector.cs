using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Vault connector that stores the vault in a local file using the browser's File System Access API.
/// The file handle is persisted in IndexedDB so it survives page reloads.
/// Only supported in Chromium-based browsers (Chrome, Edge, Opera).
/// <para>
/// Uses a two-phase access flow to preserve the browser's transient user activation:
/// 1. <see cref="PreAccessAsync"/> synchronously initiates the file picker or permission request.
/// 2. <see cref="AccessAsync"/> awaits the result via JS interop.
/// </para>
/// </summary>
public class LocalFileVaultConnector(IJSRuntime js) : IVaultConnector
{
    private bool _hasAccess;
    private bool _hasStoredHandle;

    /// <inheritdoc />
    public Guid Id { get; } = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001");

    /// <inheritdoc />
    public string Name
    {
        get
        {
            return "Local File";
        }
    }

    /// <inheritdoc />
    public string Description
    {
        get
        {
            return "Store vault in a local file. Works offline. Requires a Chromium-based browser (Chrome, Edge).";
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationEntry> Configuration { get; } = [];

    /// <inheritdoc />
    public bool CanRetryAccess
    {
        get
        {
            return _hasStoredHandle && _hasAccess == false;
        }
    }

    /// <inheritdoc />
    public string? AccessFailureMessage { get; private set; }

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            return _hasAccess || _hasStoredHandle;
        }
    }

    public void ClearSecrets()
    {
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        // Short-circuit if already loaded — this avoids an async yield on retry,
        // which would consume the browser's transient user activation before
        // AccessAsync gets a chance to use it.
        if (_hasStoredHandle || _hasAccess)
        {
            return;
        }

        // Restore the file handle from IndexedDB into the JS in-memory variable.
        // Also calls queryPermission() — if the user chose "allow every time",
        // the handle is ready to use without a gesture.
        _hasStoredHandle = await js.InvokeAsync<bool>("localFileInterop.restoreHandle", cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> AccessAsync(CancellationToken cancellationToken)
    {
        // Already accessed this session.
        if (_hasAccess)
        {
            return true;
        }

        // Synchronously initiate the file picker or permission request.
        // This MUST happen before the first await so the browser's transient
        // user activation (from a click handler) is still active.
        if (js is IJSInProcessRuntime jsInProcess)
        {
            jsInProcess.Invoke<object?>("localFileInitiateAccess");
        }

        // Now await the async result.
        string? fileName = await js.InvokeAsync<string?>("localFileInterop.awaitAccess", cancellationToken);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            if (_hasStoredHandle)
            {
                AccessFailureMessage = "The browser needs your permission to access the vault file. Click Retry to grant access.";
            }

            return false;
        }

        AccessFailureMessage = null;

        _hasAccess = true;
        return true;
    }

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync(CancellationToken cancellationToken)
    {
        if (_hasAccess == false)
        {
            throw new InvalidOperationException("No file selected. Use Access to pick a file first.");
        }

        string? content = await js.InvokeAsync<string?>("localFileInterop.readFile", cancellationToken);
        return content ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task SaveVaultAsync(string content, CancellationToken cancellationToken)
    {
        if (_hasAccess == false)
        {
            throw new InvalidOperationException("No file selected. Use Access to pick a file first.");
        }

        bool success = await js.InvokeAsync<bool>("localFileInterop.writeFile", cancellationToken, content);

        if (success == false)
        {
            throw new InvalidOperationException("Failed to write to the local file.");
        }
    }
}
