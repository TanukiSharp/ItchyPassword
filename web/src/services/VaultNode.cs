namespace ItchyPassword.App.Models;

public class VaultNode
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsFolder { get; set; }
    public VaultEntry? Entry { get; set; }
    public List<VaultNode> Children { get; set; } = new();
}
