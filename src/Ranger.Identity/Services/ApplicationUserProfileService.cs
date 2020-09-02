using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoWrapper.Wrappers;
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
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ITenantsHttpClient tenantsClient;
        private readonly IProjectsHttpClient projectsClient;
        private readonly ILogger logger;

        public ApplicationUserProfileService(Func<TenantOrganizationNameModel, RangerUserManager> userManager, IHttpContextAccessor contextAccessor, ITenantsHttpClient tenantsClient, IProjectsHttpClient projectsClient, ILogger<ApplicationUserProfileService> logger)
        {
            this.userManager = userManager;
            this.contextAccessor = contextAccessor;
            this.tenantsClient = tenantsClient;
            this.projectsClient = projectsClient;
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

            var domain = contextAccessor.HttpContext.Request.Host.GetDomainFromHost();

            var tenantApiResponse = await tenantsClient.GetTenantByDomainAsync<TenantOrganizationNameModel>(domain);
            var localUserManager = userManager(tenantApiResponse.Result);
            var user = await localUserManager.FindByIdAsync(context.Subject.GetSubjectId());
            var claims = new List<Claim> {
                new Claim ("email", user.Email),
                new Claim ("firstName", user.FirstName),
                new Claim ("lastName", user.LastName),
                new Claim ("domain", domain),
        };

            var role = await localUserManager.GetRolesAsync(user);
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
            var domain = contextAccessor.HttpContext.Request.Host.GetDomainFromHost();
            var tenantApiResponse = await tenantsClient.GetTenantByDomainAsync<TenantOrganizationNameModel>(domain);
            var localUserManager = userManager(tenantApiResponse.Result);
            var user = await localUserManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}