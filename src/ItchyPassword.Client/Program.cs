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
builder.Services.AddScoped<RandomBytePool>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IMasterKeyProvider, MasterKeyProvider>();
builder.Services.AddScoped<IAppState, AppState>();
builder.Services.AddScoped<VaultSession>();
builder.Services.AddScoped<IVaultCryptoService, VaultCryptoService>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<VaultMigrationService>();
builder.Services.AddSingleton<ErrorLogService>();

builder.Services.AddScoped<IVaultConnector, GitHubVaultConnector>();
builder.Services.AddScoped<IVaultConnector, GoogleDriveVaultConnector>();

await builder.Build().RunAsync();
