
using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
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
        // Necessary to have because we're running an IHost and need to configure the Webhost,
        // this can be set from CustomWebApplicationFactory for integration testing
        public static string HostingUrl;
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            HostingUrl = config["serverBindingUrl"];
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var rangerIdentityDbInitializer = scope.ServiceProvider.GetRequiredService<IIdentityDbContextInitializer>();
                var configurationDbInitializer = scope.ServiceProvider.GetRequiredService<IConfigurationDbContextInitializer>();
                var persistedGrantsDbInitializer = scope.ServiceProvider.GetRequiredService<IPersistedGrantDbContextInitializer>();

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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(builder =>
            {
                builder
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .UseLogging()
                .UseUrls(HostingUrl)
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory());
            });
    }
}