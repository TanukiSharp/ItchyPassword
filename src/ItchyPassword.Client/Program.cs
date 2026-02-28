using ItchyPassword.Client;
using ItchyPassword.Client.Services;
using ItchyPassword.Client.Services.VaultConnectors;
using ItchyPassword.Core.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<IMasterKeyProvider, MasterKeyProvider>();
builder.Services.AddScoped<UiState>();
builder.Services.AddScoped<VaultSession>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<VaultMigrationService>();
builder.Services.AddScoped<VaultUnlockService>();

builder.Services.AddScoped<IVaultConnector, GitHubVaultConnector>();
builder.Services.AddScoped<IVaultConnector, GoogleDriveVaultConnector>();
builder.Services.AddScoped<IVaultConnector, LocalFileVaultConnector>();

await builder.Build().RunAsync();
