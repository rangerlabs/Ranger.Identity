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
        private readonly Func<string, RangerUserManager> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ITenantsClient tenantsClient;
        private readonly IProjectsClient projectsClient;
        private readonly ILogger logger;

        public ApplicationUserProfileService(Func<string, RangerUserManager> userManager, RoleManager<IdentityRole> roleManager, IHttpContextAccessor contextAccessor, ITenantsClient tenantsClient, IProjectsClient projectsClient, ILogger<ApplicationUserProfileService> logger)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
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
            var localUserManager = userManager(domain);
            var user = await localUserManager.FindByIdAsync(context.Subject.GetSubjectId());
            var claims = new List<Claim> {
                new Claim ("email", user.Email),
                new Claim ("firstName", user.FirstName),
                new Claim ("lastName", user.LastName),
                new Claim ("authorizedProjects", JsonConvert.SerializeObject(await projectsClient.GetProjectIdsForUser(domain, user.Email)))
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
            var localUserManager = userManager(contextAccessor.HttpContext.Request.Host.GetDomainFromHost());
            var user = await localUserManager.FindByIdAsync(context.Subject.GetSubjectId());
            context.IsActive = user != null;
        }
    }
}