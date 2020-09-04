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
        private readonly IDatabase redisDb;
        private readonly ILogger<TenantServiceRangerIdentityDbContext> logger;
        private readonly CloudSqlOptions cloudSqlOptions;

        public TenantServiceRangerIdentityDbContext(IConnectionMultiplexer connectionMultiplexer, ITenantsHttpClient tenantsClient, CloudSqlOptions cloudSqlOptions, ILogger<TenantServiceRangerIdentityDbContext> logger)
        {
            this.cloudSqlOptions = cloudSqlOptions;
            this.logger = logger;
            this.tenantsClient = tenantsClient;
            redisDb = connectionMultiplexer.GetDatabase();
        }

        public (DbContextOptions<T> options, ContextTenant contextTenant) GetDbContextOptions<T>(string tenantId)
            where T : DbContext
        {
            NpgsqlConnectionStringBuilder connectionBuilder = new NpgsqlConnectionStringBuilder(cloudSqlOptions.ConnectionString);
            connectionBuilder.Username = tenantId;
            var tenantDbKey = RedisKeys.TenantDbPassword(tenantId);

            ContextTenant contextTenant = null;
            string redisValue = redisDb.StringGet(tenantDbKey);
            if (string.IsNullOrWhiteSpace(redisValue))
            {
                var apiResponse = tenantsClient.GetTenantByIdAsync<ContextTenant>(tenantId).Result;
                connectionBuilder.Password = apiResponse.Result.DatabasePassword;
                redisDb.StringSet(tenantDbKey, apiResponse.Result.DatabasePassword, TimeSpan.FromHours(1));
                contextTenant = apiResponse.Result;
            }
            else
            {
                connectionBuilder.Password = redisValue;
                contextTenant = new ContextTenant(tenantDbKey, redisValue, true);
            }

            var options = new DbContextOptionsBuilder<T>();
            options.UseNpgsql(connectionBuilder.ToString());
            return (options.Options, contextTenant);
        }
    }
}