using System.IO;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ranger.Identity.Data;

namespace Ranger.Identity.Tests
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

            Program.HostingUrl = configuration["serverBindingUrl"];

            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((context, conf) =>
            {
                conf.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables();
            });
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<ConfigurationDbContext>(options =>
                    {
                        options.UseNpgsql(configuration["cloudSql:ConnectionString"]);
                    });
                services.AddDbContext<PersistedGrantDbContext>(options =>
                    {
                        options.UseNpgsql(configuration["cloudSql:ConnectionString"]);
                    });
                services.AddDbContext<RangerIdentityDbContext>(options =>
                    {
                        options.UseNpgsql(configuration["cloudSql:ConnectionString"]);
                    });

                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var rangerIdentityDbInitializer = scope.ServiceProvider.GetRequiredService<IIdentityDbContextInitializer>();
                    var configurationDbInitializer = scope.ServiceProvider.GetRequiredService<IConfigurationDbContextInitializer>();
                    var persistedGrantsDbInitializer = scope.ServiceProvider.GetRequiredService<IPersistedGrantDbContextInitializer>();


                    rangerIdentityDbInitializer.Migrate();
                    rangerIdentityDbInitializer.EnsureRowLevelSecurityApplied();
                    rangerIdentityDbInitializer.Seed();
                    configurationDbInitializer.Migrate();
                    persistedGrantsDbInitializer.Migrate();
                }
            });
        }
    }
}