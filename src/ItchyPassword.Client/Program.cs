using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ItchyPassword.Client;
using ItchyPassword.Client.Services;
using ItchyPassword.Core.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<VaultDataService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ClientVaultState>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<VaultMigrationService>();
builder.Services.AddScoped<VaultUnlockService>();

await builder.Build().RunAsync();
