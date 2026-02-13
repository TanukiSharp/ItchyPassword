using System.Net.Http.Json;

namespace ItchyPassword.App.Services;

public interface IVaultService
{
    string? MasterKey { get; set; }
    bool IsUnlocked { get; }
    Task<string> LoadVault(string owner, string repo, string path, string token);
}

public class VaultService : IVaultService
{
    private readonly HttpClient _httpClient;

    public VaultService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string? MasterKey { get; set; }
    
    public bool IsUnlocked => !string.IsNullOrEmpty(MasterKey);

    public async Task<string> LoadVault(string owner, string repo, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/contents/{path}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("User-Agent", "ItchyPassword");
        request.Headers.Add("Accept", "application/vnd.github.v3.raw");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}
