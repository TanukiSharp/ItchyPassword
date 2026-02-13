namespace ItchyPassword.App.src.services;

public class VaultService
{
    public string? MasterKey { get; set; }

    public bool IsUnlocked => !string.IsNullOrEmpty(MasterKey);
}
