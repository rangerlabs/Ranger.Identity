using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<RangerIdentityDbContext>
    {
        public RangerIdentityDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<RangerIdentityDbContext>();
            if (args != null && args.Length > 0)
            {
                options.UseNpgsql(args[0]);

            }
            else
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                options.UseNpgsql(config["cloudSql:ConnectionString"]);
            }
            return new RangerIdentityDbContext(options.Options);
        }
    }
}