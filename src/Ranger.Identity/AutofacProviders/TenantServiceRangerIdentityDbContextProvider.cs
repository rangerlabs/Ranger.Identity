using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using System.Web;

namespace Ranger.Identity
{
    public class TenantServiceRangerIdentityDbContext : ITenantContextProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger<TenantServiceRangerIdentityDbContext> logger;
        private readonly CloudSqlOptions cloudSqlOptions;

        public TenantServiceRangerIdentityDbContext(IHttpContextAccessor httpContextAccessor, ITenantsClient tenantsClient, CloudSqlOptions cloudSqlOptions, ILogger<TenantServiceRangerIdentityDbContext> logger)
        {
            this.cloudSqlOptions = cloudSqlOptions;
            this.logger = logger;
            this.tenantsClient = tenantsClient;
            this.httpContextAccessor = httpContextAccessor;
        }

        public (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) GetDbContextOptionsFromHeader()
        {
            return getDbContextOptions(httpContextAccessor.HttpContext.Request.Headers["x-ranger-domain"].First());
        }

        public (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) GetDbContextOptions(string tenant) => getDbContextOptions(tenant);

        private (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) getDbContextOptions(string tenant)
        {
            TenantOrganizationNameModel contextTenant = null;
            try
            {
                contextTenant = this.tenantsClient.GetTenantAsync<TenantOrganizationNameModel>(tenant).Result;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                throw;
            }

            NpgsqlConnectionStringBuilder connectionBuilder = new NpgsqlConnectionStringBuilder(cloudSqlOptions.ConnectionString);
            connectionBuilder.Username = contextTenant.DatabaseUsername;
            connectionBuilder.Password = contextTenant.DatabasePassword;

            var options = new DbContextOptionsBuilder<RangerIdentityDbContext>();
            options.UseNpgsql(connectionBuilder.ToString());
            return (options.Options, contextTenant);
        }

    }
}