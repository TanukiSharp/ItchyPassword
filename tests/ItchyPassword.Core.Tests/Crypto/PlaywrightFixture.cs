using Microsoft.Playwright;

namespace ItchyPassword.Core.Tests.Crypto;

/// <summary>
/// Shared Playwright browser fixture for crypto tests.
/// Used with <see cref="IClassFixture{TFixture}"/> to avoid creating a browser per test.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; private set; } = null!;
    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    /// <summary>
    /// Creates a new browser page with crypto.js loaded and base64 marshaling helpers injected.
    /// </summary>
    public async Task<IPage> CreatePageWithCryptoAsync()
    {
        IPage page = await Browser.NewPageAsync();

        string path = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../src/ItchyPassword.Client/wwwroot/js/crypto.js"));

        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException($"Could not find crypto.js at {path}");
        }

        string jsContent = await File.ReadAllTextAsync(path);

        // Intercept all requests to serve a minimal page locally (no internet needed).
        // An https: origin is required for SubtleCrypto to be available.
        await page.RouteAsync("https://local.test/**", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<!DOCTYPE html><html><body></body></html>",
            });
        });

        await page.GotoAsync("https://local.test/crypto");
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = jsContent });

        // Inject helper functions for efficient byte-array marshaling via base64.
        await page.EvaluateAsync(@"() => {
            window.__fromB64 = function(b64) {
                const binStr = atob(b64);
                const bytes = new Uint8Array(binStr.length);
                for (let i = 0; i < binStr.length; i++) bytes[i] = binStr.charCodeAt(i);
                return bytes;
            };
            window.__toB64 = function(arr) {
                let binary = '';
                for (let i = 0; i < arr.byteLength; i++) binary += String.fromCharCode(arr[i]);
                return btoa(binary);
            };
        }");

        return page;
    }

    /// <summary>
    /// Creates a new browser page with crypto.js and passkey.js loaded, and base64 marshaling helpers injected.
    /// </summary>
    public async Task<IPage> CreatePageWithPasskeyAsync()
    {
        IPage page = await CreatePageWithCryptoAsync();

        string passkeyPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../src/ItchyPassword.Client/wwwroot/js/passkey.js"));

        if (File.Exists(passkeyPath) == false)
        {
            throw new FileNotFoundException($"Could not find passkey.js at {passkeyPath}");
        }

        string jsContent = await File.ReadAllTextAsync(passkeyPath);
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = jsContent });

        return page;
    }
}
