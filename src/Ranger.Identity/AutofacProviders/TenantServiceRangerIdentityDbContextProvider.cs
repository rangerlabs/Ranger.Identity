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
using StackExchange.Redis;

namespace Ranger.Identity
{
    public class TenantServiceRangerIdentityDbContext
    {
        private readonly ITenantsHttpClient tenantsClient;
        private readonly ILogger<TenantServiceRangerIdentityDbContext> logger;
        private readonly CloudSqlOptions cloudSqlOptions;

        public TenantServiceRangerIdentityDbContext(ITenantsHttpClient tenantsClient, CloudSqlOptions cloudSqlOptions, ILogger<TenantServiceRangerIdentityDbContext> logger)
        {
            this.cloudSqlOptions = cloudSqlOptions;
            this.logger = logger;
            this.tenantsClient = tenantsClient;
        }

        public (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel tenantOrganizationNameModel) GetDbContextOptions(TenantOrganizationNameModel tenantOrganizationNameModel)
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