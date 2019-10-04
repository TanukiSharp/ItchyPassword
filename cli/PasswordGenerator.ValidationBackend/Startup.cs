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

namespace PasswordGenerator.ValidationBackend
{
    public struct KeyDerivationData
    {
        public string privateKey { get; set; }
        public string publicKey { get; set; }
        public int iterations { get; set; }
        public string algorithmName { get; set; }
        public string alphabet { get; set; }
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

                    byte[] password = Generator.GeneratePassword(data.privateKey, data.publicKey, data.iterations, new HashAlgorithmName(data.algorithmName));
                    string encodedPassword = password.ToCustomBase(data.alphabet);

                    await context.Response.WriteAsync(encodedPassword);
                });
            });
        }
    }
}
