
using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Ranger.Monitoring.Logging;

namespace Ranger.Identity
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var host = BuildHost(config["serverBindingUrl"], args);
            using (var scope = host.Services.CreateScope())
            {
                var rangerIdentityDbInitializer = scope.ServiceProvider.GetRequiredService<IIdentityDbContextInitializer>();
                var configurationDbInitializer = scope.ServiceProvider.GetRequiredService<IConfigurationDbContextInitializer>();
                var persistedGrantsDbInitializer = scope.ServiceProvider.GetRequiredService<IPersistedGrantDbContextInitializer>();

                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                rangerIdentityDbInitializer.Migrate();
                await rangerIdentityDbInitializer.EnsureRowLevelSecurityApplied();
                await rangerIdentityDbInitializer.Seed();
                configurationDbInitializer.Migrate();
                persistedGrantsDbInitializer.Migrate();
            }
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Microsoft.Extensions.Hosting.Environments.Development)
            {
                IdentityModelEventSource.ShowPII = true;
            }
            host.Run();
        }

        public static IHost BuildHost(string serverBindingUrl, string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(builder =>
            {
                builder
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .UseUrls(serverBindingUrl)
                .UseLogging()
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory());
            })
            .Build();
    }
}