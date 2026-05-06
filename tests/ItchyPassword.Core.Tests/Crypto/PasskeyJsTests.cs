using System.Text.Json;
using Microsoft.Playwright;

namespace ItchyPassword.Core.Tests.Crypto;

public class PasskeyJsTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private ICDPSession _cdp = null!;
    private string _authenticatorId = string.Empty;

    public PasskeyJsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageWithPasskeyAsync();

        // Create CDP session to inject a virtual authenticator
        _cdp = await _page.Context.NewCDPSessionAsync(_page);
        await _cdp.SendAsync("WebAuthn.enable");

        // The "prf" extension requires CTAP2, user verification, and resident keys.
        JsonElement? cdpResult = await _cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new Dictionary<string, object>
            {
                ["protocol"] = "ctap2",
                ["transport"] = "internal",
                ["hasUserVerification"] = true,
                ["isUserVerifyingPlatformAuthenticator"] = true,
                ["automaticPresenceSimulation"] = true,
                ["hasResidentKey"] = true,
                ["hasPrf"] = true,
                ["hasHmacSecret"] = true
            }
        });

        if (cdpResult.HasValue)
        {
            _authenticatorId = cdpResult.Value.GetProperty("authenticatorId").GetString()!;
            await _cdp.SendAsync("WebAuthn.setUserVerified", new Dictionary<string, object>
            {
                ["authenticatorId"] = _authenticatorId,
                ["isUserVerified"] = true
            });
        }
    }

    public async Task DisposeAsync()
    {
        if (_cdp is not null)
        {
            await _cdp.SendAsync("WebAuthn.disable");
            await _cdp.DetachAsync();
        }
        if (_page is not null)
        {
            await _page.CloseAsync();
        }
    }

    [Fact]
    public async Task IsSupported_ReturnsTrue_WithVirtualAuthenticator()
    {
        bool supported = await _page.EvaluateAsync<bool>("ItchyPassword.Passkey.isSupported()");
        Assert.True(supported, "Expected passkey to be supported with the virtual platform authenticator.");
    }

    [Fact]
    public async Task EnrollAndWrap_Then_UnlockAndUnwrap_ReturnsSameMasterKey()
    {
        // 1. Arrange: master key
        string masterKeyB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("super-secret-master-key-that-is-long-enough"));

        // 2. Act: Enroll and Wrap
        JsonElement result = await _page.EvaluateAsync<JsonElement>(@"async (mObj) => {
            const masterBytes = window.__fromB64(mObj.mk);
            const userIdBytes = new Uint8Array([1, 2, 3, 4, 5, 6, 7, 8]);
            const res = await window.ItchyPassword.Passkey.enrollAndWrap(userIdBytes, 'Test User', masterBytes);
            return {
                credentialId: window.__toB64(res.credentialId),
                wrappedMasterKey: window.__toB64(res.wrappedMasterKey)
            };
        }", new { mk = masterKeyB64 });

        string credentialIdB64 = result.GetProperty("credentialId").GetString()!;
        string wrappedMasterKeyB64 = result.GetProperty("wrappedMasterKey").GetString()!;

        Assert.False(string.IsNullOrWhiteSpace(credentialIdB64));
        Assert.False(string.IsNullOrWhiteSpace(wrappedMasterKeyB64));

        // 3. Act: Unlock and Unwrap
        string unwrappedB64 = await _page.EvaluateAsync<string>(@"async (mObj) => {
            const credBytes = window.__fromB64(mObj.cred);
            const wrappedBytes = window.__fromB64(mObj.wrapped);
            const dec = await window.ItchyPassword.Passkey.unlockAndUnwrap(credBytes, wrappedBytes);
            return window.__toB64(dec);
        }", new { cred = credentialIdB64, wrapped = wrappedMasterKeyB64 });

        // 4. Assert
        Assert.Equal(masterKeyB64, unwrappedB64);
    }

    [Fact]
    public async Task UnlockAndUnwrap_FailsWithNotAllowedError_WhenAuthenticatorCancels()
    {
        // 1. Arrange: enroll first
        string masterKeyB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("another-master-key-here-1234"));

        JsonElement result = await _page.EvaluateAsync<JsonElement>(@"async (mObj) => {
            const masterBytes = window.__fromB64(mObj.mk);
            const userIdBytes = new Uint8Array([9, 8, 7, 6, 5, 4, 3, 2]);
            const res = await window.ItchyPassword.Passkey.enrollAndWrap(userIdBytes, 'Test User', masterBytes);
            return {
                credentialId: window.__toB64(res.credentialId),
                wrappedMasterKey: window.__toB64(res.wrappedMasterKey)
            };
        }", new { mk = masterKeyB64 });

        string credentialIdB64 = result.GetProperty("credentialId").GetString()!;
        string wrappedMasterKeyB64 = result.GetProperty("wrappedMasterKey").GetString()!;

        // 2. Simulate User Cancellation
        await _cdp.SendAsync("WebAuthn.setAutomaticPresenceSimulation", new Dictionary<string, object>
        {
            ["authenticatorId"] = _authenticatorId,
            ["enabled"] = false
        });

        // 3. Act & Assert: Unlock and Unwrap should throw
        var exception = await Assert.ThrowsAsync<PlaywrightException>(async () =>
        {
            await _page.EvaluateAsync<string>(@"async (mObj) => {
                window.ItchyPassword.Passkey._assertionTimeoutMs = 1000;

                const credBytes = window.__fromB64(mObj.cred);
                const wrappedBytes = window.__fromB64(mObj.wrapped);
                await window.ItchyPassword.Passkey.unlockAndUnwrap(credBytes, wrappedBytes);
            }", new { cred = credentialIdB64, wrapped = wrappedMasterKeyB64 });
        });

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
