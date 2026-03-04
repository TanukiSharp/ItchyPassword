using Microsoft.Playwright;

namespace ItchyPassword.Core.Tests.Crypto;

/// <summary>
/// Tests interoperability using a real browser via Playwright running the actual crypto.js code.
/// Ensures that the .NET backend can correctly decrypt secrets created by the frontend
/// and generate identical static keys.
/// </summary>
public class InteropTests : IAsyncLifetime
{
    private readonly DotNetCryptoService _crypto;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    // Test Vector
    private readonly byte[] _masterKey = System.Text.Encoding.UTF8.GetBytes("test-master-key");

    public InteropTests()
    {
        _crypto = new DotNetCryptoService();
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    private async Task<IPage> CreatePageWithCryptoAsync()
    {
        IPage page = await _browser.NewPageAsync();

        // Calculate path relative to the test output directory
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/ItchyPassword.Client/wwwroot/js/crypto.js"));

        if (!File.Exists(path))
        {
             throw new FileNotFoundException($"Could not find crypto.js at {path}");
        }

        string jsContent = await File.ReadAllTextAsync(path);

        // Inject script
        await page.GotoAsync("https://example.com");
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = jsContent });

        return page;
    }

    [Fact]
    public async Task EncryptInJS_DecryptInCSharp_ShouldMatch()
    {
        IPage page = await CreatePageWithCryptoAsync();
        string plaintext = "Hello from Playwright!";
        string keyArrayString = "[" + string.Join(",", _masterKey) + "]";

        string script = $@"
            async () => {{
                const key = new Uint8Array({keyArrayString});
                const plaintextBytes = new TextEncoder().encode('{plaintext}');
                const encrypted = await window.ItchyPassword.Crypto.encryptV3(plaintextBytes, key);

                let binary = '';
                const len = encrypted.byteLength;
                for (let i = 0; i < len; i++) {{
                    binary += String.fromCharCode(encrypted[i]);
                }}
                return btoa(binary);
            }}
        ";

        string base64Cipher = await page.EvaluateAsync<string>(script);
        byte[] cipherBytes = Convert.FromBase64String(base64Cipher);

        // Decrypt with C#
        byte[] decryptedBytes = await _crypto.DecryptV3Async(cipherBytes, _masterKey, CancellationToken.None);
        string decryptedText = System.Text.Encoding.UTF8.GetString(decryptedBytes);

        Assert.Equal(plaintext, decryptedText);
    }

    [Fact]
    public async Task EncryptInCSharp_DecryptInJS_ShouldMatch()
    {
        IPage page = await CreatePageWithCryptoAsync();
        string plaintext = "Hello from DotNet!";
        byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);

        // Encrypt with C#
        byte[] cipherBytes = await _crypto.EncryptV3Async(plaintextBytes, _masterKey, CancellationToken.None);
        string base64Cipher = Convert.ToBase64String(cipherBytes);
        string keyArrayString = "[" + string.Join(",", _masterKey) + "]";

        string script = $@"
            async () => {{
                const KEY = new Uint8Array({keyArrayString});
                const CIPHER_B64 = '{base64Cipher}';

                const cipherStr = atob(CIPHER_B64);
                const cipherBytes = new Uint8Array(cipherStr.length);
                for (let i = 0; i < cipherStr.length; i++) {{
                    cipherBytes[i] = cipherStr.charCodeAt(i);
                }}

                const decrypted = await window.ItchyPassword.Crypto.decryptV3(cipherBytes, KEY);
                return new TextDecoder().decode(decrypted);
            }}
        ";

        string decryptedText = await page.EvaluateAsync<string>(script);

        Assert.Equal(plaintext, decryptedText);
    }

    [Fact]
    public async Task GeneratePasswordV2_ShouldMatch()
    {
        IPage page = await CreatePageWithCryptoAsync();
        string publicPartStr = "example.com";
        byte[] publicPart = System.Text.Encoding.UTF8.GetBytes(publicPartStr);

        // Serialize byte[] as explicit number array to avoid base64 string issue in JS Uint8Array constructor
        string keyArrayString = "[" + string.Join(",", _masterKey) + "]";
        string publicPartArrayString = "[" + string.Join(",", publicPart) + "]";

        // Generate in C#
        byte[] csharpResult = await _crypto.GeneratePasswordV2Async(_masterKey, publicPart, purpose: "Password", CancellationToken.None);
        string csharpBase64 = Convert.ToBase64String(csharpResult);

        // Generate in JS using the loaded crypto.js library
        string script = $@"
            async () => {{
                // Use the exposed library function from crypto.js
                // window.ItchyPassword.Crypto.generatePasswordV2(privatePart, publicPart, hkdfPurpose)

                const KEY = new Uint8Array({keyArrayString});
                const PUB = new Uint8Array({publicPartArrayString});

                // generatePasswordV2(privatePart, publicPart, hkdfPurpose)
                // default purpose is 'Password' which matches C# implementation
                const result = await window.ItchyPassword.Crypto.generatePasswordV2(KEY, PUB);

                // Convert Uint8Array result to base64 for return
                let binary = '';
                const len = result.byteLength;
                const arr = new Uint8Array(result.buffer ? result.buffer : result); // Ensure array view
                for (let i = 0; i < len; i++) {{
                    binary += String.fromCharCode(arr[i]);
                }}
                return btoa(binary);
            }}
        ";

        string jsBase64 = await page.EvaluateAsync<string>(script);
        Assert.Equal(csharpBase64, jsBase64);
    }
}
