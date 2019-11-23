using System;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{
    public class ApplicationUserResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly RangerUserManager userManager;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger<ApplicationUserResourceOwnerPasswordValidator> logger;

        public ApplicationUserResourceOwnerPasswordValidator(RangerUserManager userManager, IHttpContextAccessor contextAccessor, ITenantsClient tenantsClient, ILogger<ApplicationUserResourceOwnerPasswordValidator> logger)
        {
            this.userManager = userManager;
            this.contextAccessor = contextAccessor;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
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
            var user = await userManager.FindByEmailAsync(context.UserName);
            if (await userManager.CheckPasswordAsync(user, context.Password))
            {
                context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.Password);
            }
        }
    }
}