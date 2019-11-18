using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ranger.ApiUtilities;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{
    [ApiController]
    [Authorize(IdentityServerConstants.LocalApi.PolicyName)]
    public class UserController : BaseApiController
    {
        private readonly RangerUserManager.Factory multitenantApplicationUserManagerFactory;
        private readonly RangerSignInManager.Factory signInManagerFactory;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger<UserController> logger;

        public UserController(RangerUserManager.Factory multitenantApplicationUserManagerFactory, RangerSignInManager.Factory signInManagerFactory, ITenantsClient tenantsClient, ILogger<UserController> logger)
        {
            this.multitenantApplicationUserManagerFactory = multitenantApplicationUserManagerFactory;
            this.signInManagerFactory = signInManagerFactory;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }

        [HttpPost("/user/{userId}/password-reset")]
        public async Task<IActionResult> PasswordReset([FromRoute] string userId, UserConfirmPasswordResetModel userConfirmPasswordResetModel)
        {
            ContextTenant tenant = null;
            try
            {
                tenant = await this.tenantsClient.GetTenantAsync<ContextTenant>(Domain);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                return InternalServerError();
            }

            var userManager = multitenantApplicationUserManagerFactory.Invoke(tenant);
            IdentityResult result = null;

            try
            {
                var user = await userManager.FindByIdAsync(userId);
                result = await userManager.ResetPasswordAsync(user, userConfirmPasswordResetModel.Token, userConfirmPasswordResetModel.NewPassword);
                if (result.Succeeded)
                {
                    var signInManager = this.signInManagerFactory.Invoke(tenant);
                    await signInManager.SignOutAsync();
                }
            }
            catch (Exception)
            {
                var apiErrorContent = new ApiErrorContent();
                apiErrorContent.Errors.Add("An error occurrred confirming the email address.");
                return Conflict(apiErrorContent);
            }
            return result.Succeeded ? NoContent() : StatusCode(StatusCodes.Status304NotModified);
        }

        [HttpPut("/user/{userId}/confirm")]
        public async Task<IActionResult> Confirm([FromRoute] string userId, UserConfirmModel confirmModel)
        {
            ContextTenant tenant = null;
            try
            {
                tenant = await this.tenantsClient.GetTenantAsync<ContextTenant>(Domain);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                return InternalServerError();
            }

            var userManager = multitenantApplicationUserManagerFactory.Invoke(tenant);
            IdentityResult result = null;
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                result = await userManager.ConfirmEmailAsync(user, confirmModel.RegistrationKey);
            }
            catch (Exception)
            {
                var apiErrorContent = new ApiErrorContent();
                apiErrorContent.Errors.Add("An error occurrred confirming the email address.");
                return Conflict(apiErrorContent);
            }
            return result.Succeeded ? NoContent() : StatusCode(StatusCodes.Status304NotModified);
        }

        [HttpGet("/user/{username}")]
        [TenantDomainRequired]
        public async Task<IActionResult> Index(string username)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { errors = $"{nameof(username)} cannot be null or empty." });
            }

            ContextTenant tenant = null;
            try
            {
                tenant = await this.tenantsClient.GetTenantAsync<ContextTenant>(Domain);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                return InternalServerError();
            }

            var userManager = multitenantApplicationUserManagerFactory.Invoke(tenant);
            var user = await userManager.FindByEmailAsync(username);
            if (user is null)
            {
                return NotFound();
            }
            var role = await userManager.GetRolesAsync(user);
            return Ok(MapUserToUserResponse(user, role.First()));
        }

        [HttpGet("/user/all")]
        [TenantDomainRequired]
        public async Task<IActionResult> All()
        {
            ContextTenant tenant = null;
            try
            {
                tenant = await this.tenantsClient.GetTenantAsync<ContextTenant>(Domain);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                return InternalServerError();
            }

            var userManager = multitenantApplicationUserManagerFactory.Invoke(tenant);
            var users = await userManager.Users.ToListAsync();
            if (users is null)
            {
                return NoContent();
            }
            var userResponse = new List<ApplicationUserResponseModel>();
            foreach (var user in users)
            {
                var role = await userManager.GetRolesAsync(user);
                userResponse.Add(MapUserToUserResponse(user, role.First()));
            }
            return Ok(userResponse);
        }

        [NonAction]
        private ApplicationUserResponseModel MapUserToUserResponse(RangerUser user, string role)
        {
            return new ApplicationUserResponseModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed,
                Role = role,
            };
        }

    }
}