using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ranger.Identity {
    public class DesignTimeConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext> {
        public ConfigurationDbContext CreateDbContext (string[] args) {
            var migrationsAssembly = typeof (Startup).GetTypeInfo ().Assembly.GetName ().Name;

            var options = new DbContextOptionsBuilder<ConfigurationDbContext> ();

            if (args != null && args.Length > 0) {
                options.UseNpgsql (args[0], npgsqlOptions => {
                    npgsqlOptions.MigrationsAssembly (migrationsAssembly);
                });
            } else {
                var config = new ConfigurationBuilder ()
                    .SetBasePath (System.IO.Directory.GetCurrentDirectory ())
                    .AddJsonFile ("appsettings.json")
                    .Build ();

                options.UseNpgsql (config["cloudSql:ConnectionString"], npgsqlOptions => {
                    npgsqlOptions.MigrationsAssembly (migrationsAssembly);
                });
            }
            return new ConfigurationDbContext (options.Options, new ConfigurationStoreOptions ());
        }
    }
}