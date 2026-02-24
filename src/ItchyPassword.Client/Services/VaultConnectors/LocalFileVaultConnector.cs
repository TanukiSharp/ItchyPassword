using ItchyPassword.Core.Models;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Vault connector that stores the vault in a local file using the browser's File System Access API.
/// The file handle is persisted in IndexedDB so it survives page reloads.
/// Only supported in Chromium-based browsers (Chrome, Edge, Opera).
/// <para>
/// Uses a two-phase connect flow to preserve the browser's transient user activation:
/// 1. <see cref="PreConnectAsync"/> synchronously initiates the file picker or permission request.
/// 2. <see cref="ConnectAsync"/> awaits the result via JS interop.
/// </para>
/// </summary>
public class LocalFileVaultConnector : IVaultConnector
{
    private readonly IJSRuntime _js;

    private bool _connected;
    private bool _hasStoredHandle;

    public LocalFileVaultConnector(IJSRuntime js)
    {
        _js = js;
    }

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
    public bool CanRetryConnect
    {
        get
        {
            return _hasStoredHandle && _connected == false;
        }
    }

    /// <inheritdoc />
    public string? ConnectFailureMessage { get; private set; }

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            return _connected || _hasStoredHandle;
        }
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync()
    {
        // Short-circuit if already loaded — this avoids an async yield on retry,
        // which would consume the browser's transient user activation before
        // ConnectAsync gets a chance to use it.
        if (_hasStoredHandle || _connected)
        {
            return;
        }

        // Restore the file handle from IndexedDB into the JS in-memory variable.
        // Also calls queryPermission() — if the user chose "allow every time",
        // the handle is ready to use without a gesture.
        _hasStoredHandle = await _js.InvokeAsync<bool>("localFileInterop.restoreHandle");
    }

    /// <inheritdoc />
    public Task SaveConfigurationAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync()
    {
        // Already connected this session.
        if (_connected)
        {
            return true;
        }

        // Synchronously initiate the file picker or permission request.
        // This MUST happen before the first await so the browser's transient
        // user activation (from a click handler) is still active.
        if (_js is IJSInProcessRuntime jsInProcess)
        {
            jsInProcess.Invoke<object?>("eval", "localFileInterop.initiateConnect()");
        }

        // Now await the async result.
        string? fileName = await _js.InvokeAsync<string?>("localFileInterop.awaitConnect");

        if (string.IsNullOrWhiteSpace(fileName))
        {
            if (_hasStoredHandle)
            {
                ConnectFailureMessage = "The browser needs your permission to access the vault file. Click Retry to grant access.";
            }

            return false;
        }

        ConnectFailureMessage = null;

        _connected = true;
        return true;
    }

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync()
    {
        if (_connected == false)
        {
            throw new InvalidOperationException("No file selected. Use Connect to pick a file first.");
        }

        string? content = await _js.InvokeAsync<string?>("localFileInterop.readFile");
        return content ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task SaveVaultAsync(string content)
    {
        if (_connected == false)
        {
            throw new InvalidOperationException("No file selected. Use Connect to pick a file first.");
        }

        bool success = await _js.InvokeAsync<bool>("localFileInterop.writeFile", content);

        if (success == false)
        {
            throw new InvalidOperationException("Failed to write to the local file.");
        }
    }
}
