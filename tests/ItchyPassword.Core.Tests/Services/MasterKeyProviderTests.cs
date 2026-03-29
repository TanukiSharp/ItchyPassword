using ItchyPassword.Core.Services;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="MasterKeyProvider"/> property management and change notifications.
/// </summary>
public sealed class MasterKeyProviderTests
{
    // ── Initial state ──────────────────────────────────────────────────

    [Fact]
    public void InitialState_HasNoMasterKey()
    {
        var provider = new MasterKeyProvider();

        Assert.False(provider.HasMasterKey);
        Assert.Empty(provider.MasterKey);
    }

    // ── Set and get ────────────────────────────────────────────────────

    [Fact]
    public void SetMasterKey_UpdatesHasMasterKey()
    {
        var provider = new MasterKeyProvider
        {
            MasterKey = [1, 2, 3]
        };

        Assert.True(provider.HasMasterKey);
    }

    [Fact]
    public void SetMasterKey_ReturnsSetValue()
    {
        var provider = new MasterKeyProvider();
        byte[] key = [10, 20, 30];

        provider.MasterKey = key;

        Assert.Equal(key, provider.MasterKey);
    }

    // ── Clear (set to empty) ───────────────────────────────────────────

    [Fact]
    public void ClearMasterKey_ResetsHasMasterKey()
    {
        var provider = new MasterKeyProvider
        {
            MasterKey = [1, 2, 3]
        };

        provider.MasterKey = [];

        Assert.False(provider.HasMasterKey);
    }

    // ── PropertyChanged notifications ──────────────────────────────────

    [Fact]
    public void SetMasterKey_RaisesPropertyChanged()
    {
        var provider = new MasterKeyProvider();
        List<string?> changedProperties = [];
        provider.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        provider.MasterKey = [1, 2, 3];

        Assert.Contains(nameof(MasterKeyProvider.MasterKey), changedProperties);
        Assert.Contains(nameof(MasterKeyProvider.HasMasterKey), changedProperties);
    }

    [Fact]
    public void SetSameKey_DoesNotRaisePropertyChanged()
    {
        var provider = new MasterKeyProvider
        {
            MasterKey = [1, 2, 3]
        };

        List<string?> changedProperties = [];
        provider.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        provider.MasterKey = [1, 2, 3];

        Assert.Empty(changedProperties);
    }

    [Fact]
    public void SetDifferentKey_RaisesPropertyChanged()
    {
        var provider = new MasterKeyProvider
        {
            MasterKey = [1, 2, 3]
        };

        List<string?> changedProperties = [];
        provider.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        provider.MasterKey = [4, 5, 6];

        Assert.Contains(nameof(MasterKeyProvider.MasterKey), changedProperties);
    }

    [Fact]
    public void ClearKey_RaisesHasMasterKeyChanged()
    {
        var provider = new MasterKeyProvider
        {
            MasterKey = [1, 2, 3]
        };

        List<string?> changedProperties = [];
        provider.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        provider.MasterKey = [];

        Assert.Contains(nameof(MasterKeyProvider.HasMasterKey), changedProperties);
    }

    // ── Old key material is zeroed ─────────────────────────────────────

    [Fact]
    public void SetNewKey_ZerosOutOldKeyMaterial()
    {
        var provider = new MasterKeyProvider();
        byte[] oldKey = [1, 2, 3, 4, 5];
        provider.MasterKey = oldKey;

        provider.MasterKey = [10, 20, 30];

        // The old key array should have been zeroed out for security.
        Assert.All(oldKey, b => Assert.Equal(0, b));
    }
}
