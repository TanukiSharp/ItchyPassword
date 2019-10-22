using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PasswordGenerator.Core;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace PasswordGenerator.ValidationBackend
{
    public struct KeyDerivationData
    {
        [JsonPropertyName("privateKey")]
        public string PrivateKey { get; set; }
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }
        [JsonPropertyName("iterations")]
        public int Iterations { get; set; }
        [JsonPropertyName("algorithmName")]
        public string AlgorithmName { get; set; }
        [JsonPropertyName("alphabet")]
        public string Alphabet { get; set; }
        [JsonPropertyName("hkdfPurpose")]
        public string HkdfPurpose { get; set; }
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
                    KeyDerivationData data = await JsonSerializer.DeserializeAsync<KeyDerivationData>(context.Request.Body);
                    
                    byte[] password = Generator.GeneratePassword(data.PrivateKey, data.PublicKey, data.Iterations, new HashAlgorithmName(data.AlgorithmName), data.HkdfPurpose);
                    string encodedPassword = password.ToCustomBase(data.Alphabet);

                    await context.Response.WriteAsync(encodedPassword);
                });
            });
        }
    }
}
