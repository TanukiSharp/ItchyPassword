namespace ItchyPassword.App.Models;

public class VaultEntry
{
    public string? Public { get; set; }
    public string? Alphabet { get; set; }
    public int? Length { get; set; }
    public int? Version { get; set; }
    public Dictionary<string, object>? CustomKeys { get; set; }
}
