using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerUserStore : UserStore<RangerUser,
                                             IdentityRole,
                                             RangerIdentityDbContext,
                                             string>
    {
        public delegate IUserStore<RangerUser> Factory(ContextTenant contextTenant);

        public RangerUserStore(RangerIdentityDbContext context)
            : base(context)
        { }

        public RangerUserStore(ContextTenant contextTenant, RangerIdentityDbContext.Factory applicationDbContextFactory, CloudSqlOptions cloudSqlOptions)
            : base(
                applicationDbContextFactory.Invoke(
                    RangerUserStore.ApplicationDbContextOptionsBuilder(
                        contextTenant, cloudSqlOptions
                    )
                )
            )
        { }

        private static DbContextOptions<RangerIdentityDbContext> ApplicationDbContextOptionsBuilder(ContextTenant contextTenant, CloudSqlOptions cloudSqlOptions)
        {
            NpgsqlConnectionStringBuilder connectionBuilder = new NpgsqlConnectionStringBuilder(cloudSqlOptions.ConnectionString);
            connectionBuilder.Username = contextTenant.DatabaseUsername;
            connectionBuilder.Password = contextTenant.DatabasePassword;

            var options = new DbContextOptionsBuilder<RangerIdentityDbContext>();
            options.UseNpgsql(connectionBuilder.ToString());
            return options.Options;
        }
    }
}