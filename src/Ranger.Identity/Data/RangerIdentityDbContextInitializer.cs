using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerIdentityDbContextInitializer : IIdentityDbContextInitializer
    {
        private readonly RangerIdentityDbContext context;
        private readonly RoleManager<IdentityRole> roleManager;

        public RangerIdentityDbContextInitializer(RangerIdentityDbContext context)
        {
            this.context = context;
            roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context), null, new UpperInvariantLookupNormalizer(), null, null);
        }

        public bool EnsureCreated()
        {
            return context.Database.EnsureCreated();
        }

        public void Migrate()
        {
            context.Database.Migrate();
        }

        public async Task Seed()
        {
            foreach (var roleValue in Enum.GetValues(typeof(RolesEnum)))
            {
                var roleName = Enum.GetName(typeof(RolesEnum), roleValue);
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task EnsureRowLevelSecurityApplied()
        {
            var tables = Enum.GetNames(typeof(RowLevelSecureTablesEnum));
            var loginRoleRepository = new LoginRoleRepository<RangerIdentityDbContext>(context);
            foreach (var table in tables)
            {
                await loginRoleRepository.CreateTenantRlsPolicy(table);
            }
        }
    }
}

public interface IIdentityDbContextInitializer
{
    bool EnsureCreated();
    void Migrate();
    Task Seed();
    Task EnsureRowLevelSecurityApplied();
}