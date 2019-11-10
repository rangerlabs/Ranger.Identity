using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{
    public class ApplicationUserProfileService : IProfileService
    {
        private readonly RangerUserManager.Factory multitenantApplicationUserRepositoryFactory;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger logger;

        public ApplicationUserProfileService(RangerUserManager.Factory multitenantApplicationUserRepositoryFactory, RoleManager<IdentityRole> roleManager, IHttpContextAccessor contextAccessor, ITenantsClient tenantsClient, ILogger<ApplicationUserProfileService> logger)
        {
            this.multitenantApplicationUserRepositoryFactory = multitenantApplicationUserRepositoryFactory;
            this.roleManager = roleManager;
            this.contextAccessor = contextAccessor;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();

            logger.LogDebug("Get profile called for subject {subject} from client {client} with claim types {claimTypes} via {caller}",
                context.Subject.GetSubjectId(),
                context.Client.ClientName ?? context.Client.ClientId,
                context.RequestedClaimTypes,
                context.Caller);

            ContextTenant tenant = null;
            try
            {
                tenant = await tenantsClient.GetTenantAsync<ContextTenant>(contextAccessor.HttpContext.Request.Host.GetDomainFromHost());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                throw;
            }

            var userManager = multitenantApplicationUserRepositoryFactory.Invoke(tenant);
            var user = await userManager.FindByIdAsync(context.Subject.GetSubjectId());
            var claims = new List<Claim> {
                new Claim ("email", user.Email),
                new Claim ("firstName", user.FirstName),
                new Claim ("lastName", user.LastName),
                new Claim ("authorizedProjects", JsonConvert.SerializeObject(user.AuthorizedProjects))
            };

            var role = await userManager.GetRolesAsync(user);
            var userRole = ((RolesEnum)Enum.Parse(typeof(RolesEnum), role.First())).GetCascadedRoles();
            foreach (var r in userRole)
            {
                claims.Add(new Claim("role", r));
            }
            context.IssuedClaims.AddRange(claims);

        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            ContextTenant tenant = null;
            try
            {
                tenant = await tenantsClient.GetTenantAsync<ContextTenant>(contextAccessor.HttpContext.Request.Host.GetDomainFromHost());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                throw;
            }

            var userManager = multitenantApplicationUserRepositoryFactory.Invoke(tenant);
            var user = await userManager.FindByIdAsync(context.Subject.GetSubjectId());
            context.IsActive = user != null;
        }
    }
}