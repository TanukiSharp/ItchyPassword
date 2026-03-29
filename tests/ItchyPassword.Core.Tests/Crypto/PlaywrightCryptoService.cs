using ItchyPassword.Core.Services;
using Microsoft.Playwright;

namespace ItchyPassword.Core.Tests.Crypto;

/// <summary>
/// <see cref="ICryptoService"/> implementation that delegates all operations
/// to the browser's SubtleCrypto via Playwright, executing the actual crypto.js code.
/// Requires a page pre-configured by <see cref="PlaywrightFixture.CreatePageWithCryptoAsync"/>.
/// </summary>
public sealed class PlaywrightCryptoService(IPage page) : ICryptoService
{
    public async Task<byte[]> EncryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        string inputB64 = Convert.ToBase64String(input);
        string passwordB64 = Convert.ToBase64String(password);

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const input = __fromB64('{inputB64}');
                const password = __fromB64('{passwordB64}');
                const encrypted = await window.ItchyPassword.Crypto.encryptV3(input, password);
                return __toB64(encrypted);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> DecryptV2Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        string inputB64 = Convert.ToBase64String(input);
        string passwordB64 = Convert.ToBase64String(password);

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const input = __fromB64('{inputB64}');
                const password = __fromB64('{passwordB64}');
                const decrypted = await window.ItchyPassword.Crypto.decryptV2(input, password);
                return __toB64(decrypted);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> DecryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        string inputB64 = Convert.ToBase64String(input);
        string passwordB64 = Convert.ToBase64String(password);

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const input = __fromB64('{inputB64}');
                const password = __fromB64('{passwordB64}');
                const decrypted = await window.ItchyPassword.Crypto.decryptV3(input, password);
                return __toB64(decrypted);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart, CancellationToken cancellationToken)
    {
        string privateB64 = Convert.ToBase64String(privatePart);
        string publicB64 = Convert.ToBase64String(publicPart);

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const privatePart = __fromB64('{privateB64}');
                const publicPart = __fromB64('{publicB64}');
                const hash = await window.ItchyPassword.Crypto.generatePasswordV1(privatePart, publicPart);
                return __toB64(hash);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose, CancellationToken cancellationToken)
    {
        string privateB64 = Convert.ToBase64String(privatePart);
        string publicB64 = Convert.ToBase64String(publicPart);
        string escapedPurpose = purpose.Replace("\\", "\\\\").Replace("'", "\\'");

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const privatePart = __fromB64('{privateB64}');
                const publicPart = __fromB64('{publicB64}');
                const hash = await window.ItchyPassword.Crypto.generatePasswordV2(privatePart, publicPart, '{escapedPurpose}');
                return __toB64(hash);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> GenerateRandomBytesAsync(int count, CancellationToken cancellationToken)
    {
        string result = await page.EvaluateAsync<string>($@"
            () => {{
                const bytes = window.ItchyPassword.Crypto.generateRandomBytes({count});
                return __toB64(bytes);
            }}
        ");

        return Convert.FromBase64String(result);
    }

    public async Task<byte[]> ComputeHmacSha512Async(byte[] data, byte[] key, CancellationToken cancellationToken)
    {
        string dataB64 = Convert.ToBase64String(data);
        string keyB64 = Convert.ToBase64String(key);

        string result = await page.EvaluateAsync<string>($@"
            async () => {{
                const data = __fromB64('{dataB64}');
                const key = __fromB64('{keyB64}');
                const hmac = await window.ItchyPassword.Crypto.computeHmacSha512(data, key);
                return __toB64(hmac);
            }}
        ");

        return Convert.FromBase64String(result);
    }
}
