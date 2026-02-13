using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ItchyPassword.App;
using ItchyPassword.App.src.services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<PasswordGeneratorService>();
builder.Services.AddScoped<VaultService>();

await builder.Build().RunAsync();
