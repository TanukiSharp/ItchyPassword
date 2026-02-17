namespace ItchyPassword.Core.Models;

public enum VaultItemType
{
    Password = 0,
    Cipher = 1
}

public class VaultItem
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required VaultItemType Type { get; set; }

    // For "Cipher": The encrypted string.
    // For "Password": The public part (salt), stored in clear.
    public string Content { get; set; } = "";

    // Additional parameters for generation (e.g. alphabet, length, version)
    // Stored as Dictionary<string, string>
    public Dictionary<string, object> Parameters { get; } = [];

    public Dictionary<string, string>? Metadata { get; set; }

    public required DateTimeOffset LastModified { get; set; }
}

public class Vault
{
    public required int Version { get; init; }
    public required List<VaultItem> Items { get; init; }
}
