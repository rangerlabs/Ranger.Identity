using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWrapper.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{
    public class TenantServiceRangerIdentityDbContext
    {
        private readonly TenantsHttpClient tenantsClient;
        private readonly ILogger<TenantServiceRangerIdentityDbContext> logger;
        private readonly CloudSqlOptions cloudSqlOptions;

        public TenantServiceRangerIdentityDbContext(TenantsHttpClient tenantsClient, CloudSqlOptions cloudSqlOptions, ILogger<TenantServiceRangerIdentityDbContext> logger)
        {
            this.cloudSqlOptions = cloudSqlOptions;
            this.logger = logger;
            this.tenantsClient = tenantsClient;
        }

        public (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) GetDbContextOptionsByDomain(string domain)
        {
            try
            {
                var apiResponse = this.tenantsClient.GetTenantByDomainAsync<TenantOrganizationNameModel>(domain).Result;
                this.logger.LogError("Received {StatusCode} when attempting to retrieve the tenant. Cannot construct the tenant specific repository.", apiResponse.StatusCode);
                throw new ApiException("Failed to retrieve tenant");
            }
            catch (ApiException ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object from the Tenants service. Cannot construct the tenant specific repository.");
                throw;
            }
        }

        public (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) GetDbContextOptionsByTenantId(string tenantId)
        {
            var apiResponse = this.tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId).Result;
            this.logger.LogError("Received {StatusCode} when attempting to retrieve the tenant. Cannot construct the tenant specific repository.", apiResponse.StatusCode);
            throw new ApiException("Failed to retrieve tenant");
        }

        private (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel tenantOrganizationNameModel) getDbContextOptions(TenantOrganizationNameModel tenantOrganizationNameModel)
        {
            NpgsqlConnectionStringBuilder connectionBuilder = new NpgsqlConnectionStringBuilder(cloudSqlOptions.ConnectionString);
            connectionBuilder.Username = tenantOrganizationNameModel.TenantId;
            connectionBuilder.Password = tenantOrganizationNameModel.DatabasePassword;

            var options = new DbContextOptionsBuilder<RangerIdentityDbContext>();
            options.UseNpgsql(connectionBuilder.ToString());
            return (options.Options, tenantOrganizationNameModel);
        }

    }
}