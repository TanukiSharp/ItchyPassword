using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ItchyPassword.App;
using ItchyPassword.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();

await builder.Build().RunAsync();
