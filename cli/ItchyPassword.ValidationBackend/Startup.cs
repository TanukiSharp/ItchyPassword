using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ItchyPassword.Core;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text;

namespace ItchyPassword.ValidationBackend
{
    public struct KeyDerivationData
    {
        [JsonPropertyName("privatePart")]
        public string PrivatePart { get; set; }
        [JsonPropertyName("publicPart")]
        public string PublicPart { get; set; }
        [JsonPropertyName("alphabet")]
        public string Alphabet { get; set; }
        [JsonPropertyName("frontendClear")]
        public string FrontendClear { get; set; }
        [JsonPropertyName("frontendEncrypted")]
        public string FrontendEncrypted { get; set; }
    }

    public struct KeyDerivationDataResponse
    {
        [JsonPropertyName("generatedPassword")]
        public string GeneratedPassword { get; set; }
        [JsonPropertyName("backendClear")]
        public string BackendClear { get; set; }
        [JsonPropertyName("backendEncrypted")]
        public string BackendEncrypted { get; set; }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("meh", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseCors("meh");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    KeyDerivationData frontendData = await JsonSerializer.DeserializeAsync<KeyDerivationData>(context.Request.Body);

                    byte[] passwordBytes = Crypto.GeneratePassword(frontendData.PrivatePart.FromBase16(), frontendData.PublicPart.FromBase16(), "Password");

                    byte[] decryptedFrontendEncryptedBytes = Crypto.Decrypt(frontendData.FrontendEncrypted.FromBase16(), passwordBytes);
                    if (decryptedFrontendEncryptedBytes.ToBase16() != frontendData.FrontendClear)
                    {
                        context.Response.StatusCode = 500;
                        return;
                    }

                    string generatedPassword = passwordBytes.ToCustomBase(frontendData.Alphabet);

                    byte[] randomClearBytes = new byte[256];
                    RandomNumberGenerator.Create().GetBytes(randomClearBytes);

                    var response = new KeyDerivationDataResponse
                    {
                        GeneratedPassword = generatedPassword,
                        BackendClear = randomClearBytes.ToBase16(),
                        BackendEncrypted = Crypto.Encrypt(randomClearBytes, passwordBytes).ToBase16()
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                });
            });
        }
    }
}
