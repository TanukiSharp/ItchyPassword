using ItchyPassword.Core.Services;

namespace ItchyPassword.Client.Services.VaultConnectors
{
    /// <summary>
    /// Connector for interacting with a password vault stored in Google Drive.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="GoogleDriveVaultConnector"/> class.
    /// </remarks>
    /// <param name="http">The HTTP client instance.</param>
    /// <param name="storage">The storage service instance.</param>
    /// <param name="crypto">The crypto service instance for encrypting/decrypting secrets.</param>
    /// <param name="state">The client vault state providing the master key.</param>
    public class GoogleDriveVaultConnector(HttpClient http, LocalStorageService storage, ICryptoService crypto, ClientVaultState state) : IVaultConnector
    {
        /// <inheritdoc />
        public string Name
        {
            get
            {
                return "Google Drive (Coming Soon)";
            }
        }

        /// <inheritdoc />
        public string Description
        {
            get
            {
                return "Store vault in your Google Drive.";
            }
        }

        /// <inheritdoc />
        public Guid Id
        {
            get
            {
                return Guid.Parse("b7d4c22a-0498-4c12-a1f4-5f80e9a5c8e2");
            }
        }

        /// <inheritdoc />
        public Dictionary<string, string> Configuration { get; } = new Dictionary<string, string>
        {
            ["Token"] = "",
            ["FolderId"] = ""
        };

        /// <inheritdoc />
        public bool IsConfigured
        {
            get
            {
                return string.IsNullOrEmpty(Configuration.GetValueOrDefault("Token")) == false;
            }
        }

        /// <inheritdoc />
        public async Task LoadConfigurationAsync()
        {
            string? token = await storage.GetItemAsync("itchy_gd_token");
            string? folderId = await storage.GetItemAsync("itchy_gd_folder");

            if (string.IsNullOrEmpty(token) == false)
            {
                Configuration["Token"] = state.HasMasterKey
                    ? await VaultConnectorHelper.DecryptIfNeededAsync(token, state.MasterKey, crypto)
                    : token;
            }
            if (string.IsNullOrEmpty(folderId) == false)
            {
                Configuration["FolderId"] = folderId;
            }
        }

        /// <inheritdoc />
        public async Task SaveConfigurationAsync()
        {
            if (Configuration.TryGetValue("Token", out string? token) && string.IsNullOrWhiteSpace(token) == false)
            {
                // Only persist the token if it can be encrypted with the master key.
                if (state.HasMasterKey)
                {
                    string valueToStore = await VaultConnectorHelper.EncryptAsync(token, state.MasterKey, crypto);
                    await storage.SetItemAsync("itchy_gd_token", valueToStore);
                }
            }
            if (Configuration.TryGetValue("FolderId", out string? folderId))
            {
                await storage.SetItemAsync("itchy_gd_folder", folderId);
            }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync()
        {
            // TODO: Implement Google Auth (Implicit Flow or similar)
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<string> LoadVaultAsync()
        {
            throw new NotImplementedException("Google Drive Load not implemented yet.");
        }

        /// <inheritdoc />
        public Task SaveVaultAsync(string content)
        {
            throw new NotImplementedException("Google Drive Save not implemented yet.");
        }
    }
}
