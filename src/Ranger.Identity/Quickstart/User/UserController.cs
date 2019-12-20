using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ranger.ApiUtilities;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [ApiController]
    [Authorize(IdentityServerConstants.LocalApi.PolicyName)]
    public class UserController : BaseApiController
    {
        private readonly Func<string, RangerUserManager> userManager;
        private readonly SignInManager<RangerUser> signInManager;
        private readonly IBusPublisher _busPublisher;
        private readonly ITenantsClient _tenantsClient;
        private readonly ILogger<UserController> logger;

        public UserController(
                IBusPublisher busPublisher,
                Func<string, RangerUserManager> userManager,
                SignInManager<RangerUser> signInManager,
                ITenantsClient tenantsClient,
                ILogger<UserController> logger
            )
        {
            this._busPublisher = busPublisher;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this._tenantsClient = tenantsClient;
            this.logger = logger;
        }

        [HttpPut("/user/{username}")]
        [TenantDomainRequired]
        public async Task<IActionResult> AccountUpdate([FromRoute] string username, AccountUpdateModel accountInfoModel)
        {
            var localUserManager = userManager(Domain);
            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(username);

            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred retrieving the user.");
                return InternalServerError();
            }
            if (user is null)
            {
                return NotFound();
            }
            user.LastName = accountInfoModel.LastName;
            user.FirstName = accountInfoModel.FirstName;
            var result = await localUserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                logger.LogError($"Failed to update user {username}. Errors: {String.Join(';', result.Errors.Select(_ => _.Description).ToList())}.");
                return InternalServerError();
            }
            return Ok();
        }

        [HttpDelete("/user/{email}/account")]
        [TenantDomainRequired]
        public async Task<IActionResult> DeleteAccount([FromRoute] string email, AccountDeleteModel accountDeleteModel)
        {
            var localUserManager = userManager(Domain);
            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(email);
                var result = await localUserManager.CheckPasswordAsync(user, accountDeleteModel.Password);
                if (!result)
                {
                    var apiErrorContent = new ApiErrorContent();
                    apiErrorContent.Errors.Add("The password was invalid.");
                    return BadRequest(apiErrorContent);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred retrieving the user or users roles.");
                return InternalServerError();
            }

            if (user is null)
            {
                return NotFound();
            }

            await localUserManager.DeleteAsync(user);
            return NoContent();
        }

        [HttpDelete("/user/{email}")]
        [TenantDomainRequired]
        public async Task<IActionResult> DeleteUserByEmail([FromRoute] string email, DeleteUserModel deleteUserModel)
        {
            var localUserManager = userManager(Domain);
            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(email);
                var commandingUser = (await localUserManager.FindByEmailAsync(deleteUserModel.CommandingUserEmail));
                if (!await AssignmentValidator.ValidateAsync(commandingUser, user, localUserManager))
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred retrieving the user or users roles.");
                return InternalServerError();
            }

            if (user is null)
            {
                return NotFound();
            }

            await localUserManager.DeleteAsync(user);
            return NoContent();
        }

        [HttpPut("/user/{username}/email-change")]
        [TenantDomainRequired]
        public async Task<IActionResult> PutPasswordResetRequest([FromRoute] string username, EmailChangeModel emailChangeModel)
        {
            var localUserManager = userManager(Domain);
            RangerUser user = null;
            RangerUser conflictingUser = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(username);
                conflictingUser = await localUserManager.FindByEmailAsync(emailChangeModel.Email);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred retrieving the user.");
                return InternalServerError();
            }
            if (user is null)
            {
                return NotFound();
            }
            if (conflictingUser != null)
            {
                var apiErrorContent = new ApiErrorContent();
                apiErrorContent.Errors.Add("The requested email address is unavailable.");
                return Conflict();
            }

            user.UnconfirmedEmail = emailChangeModel.Email;
            await localUserManager.UpdateAsync(user);
            var token = HttpUtility.UrlEncode(await localUserManager.GenerateChangeEmailTokenAsync(user, emailChangeModel.Email));
            _busPublisher.Send(new SendChangeEmailEmail(user.FirstName, emailChangeModel.Email, Domain, user.Id, localUserManager.TenantOrganizationNameModel.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(username));
            return NoContent();
        }

        [HttpPost("/user/{userId}/password-reset")]
        public async Task<IActionResult> PasswordReset([FromRoute] string userId, UserConfirmPasswordResetModel userConfirmPasswordResetModel)
        {
            var localUserManager = userManager(Domain);
            IdentityResult result = null;

            try
            {
                var user = await localUserManager.FindByIdAsync(userId);
                try
                {
                    result = await localUserManager.ResetPasswordAsync(user, userConfirmPasswordResetModel.Token, userConfirmPasswordResetModel.NewPassword);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to set password for user '{user.Email}'.");
                    throw;
                }
                if (result.Succeeded)
                {
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        try
                        {
                            await localUserManager.UpdateAsync(user);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Failed to mark user '{user.Email}' as confirmed.");
                            throw;
                        }
                    }
                    await localUserManager.UpdateSecurityStampAsync(user);
                }
            }
            catch (Exception)
            {
                var apiErrorContent = new ApiErrorContent();
                apiErrorContent.Errors.Add("An error occurrred setting the user's password.");
                return Conflict(apiErrorContent);
            }
            return result.Succeeded ? NoContent() : StatusCode(StatusCodes.Status304NotModified);
        }

        [HttpPost("/user/{userId}/email-change")]
        public async Task<IActionResult> EmailChange([FromRoute] string userId, UserConfirmEmailChangeModel userConfirmEmailChangeModel)
        {
            var localUserManager = userManager(Domain);

            try
            {
                var user = await localUserManager.FindByIdAsync(userId);

                if ((await localUserManager.ChangeEmailAsync(user, user.UnconfirmedEmail, userConfirmEmailChangeModel.Token)).Succeeded)
                {
                    user.UserName = user.UnconfirmedEmail;
                    user.UnconfirmedEmail = "";
                    await localUserManager.UpdateAsync(user);
                    await localUserManager.UpdateSecurityStampAsync(user);
                }
                else
                {
                    var apiErrorContent = new ApiErrorContent();
                    apiErrorContent.Errors.Add("Ensure the provided current email and token are correct.");
                    return BadRequest(apiErrorContent);
                }
            }
            catch (DbUpdateException ex)
            {
                var postgresException = ex.InnerException as PostgresException;
                if (postgresException.SqlState == "23505")
                {
                    var apiErrorContent = new ApiErrorContent();
                    apiErrorContent.Errors.Add("The requested email is already in use.");
                    return Conflict(apiErrorContent);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurrred confirming the email address.");
                return InternalServerError();
            }
            return NoContent();
        }

        [HttpPut("/user/{userId}/confirm")]
        public async Task<IActionResult> Confirm([FromRoute] string userId, UserConfirmModel confirmModel)
        {
            var localUserManager = userManager(Domain);
            IdentityResult confirmResult = null;
            IdentityResult passwordSetResult = null;

            try
            {
                var user = await localUserManager.FindByIdAsync(userId);
                confirmResult = await localUserManager.ConfirmEmailAsync(user, confirmModel.Token);
                if (confirmResult.Succeeded)
                {
                    passwordSetResult = await localUserManager.ChangePasswordAsync(user, GlobalConfig.TempPassword, confirmModel.NewPassword);
                    if (!passwordSetResult.Succeeded)
                    {
                        logger.LogError($"Failed to set password for user '{user.Email}' after confirming their account.");
                    }
                }
            }
            catch (Exception)
            {
                var apiErrorContent = new ApiErrorContent();
                apiErrorContent.Errors.Add("An error occurrred confirming the email address.");
                return Conflict(apiErrorContent);
            }
            if (confirmResult.Succeeded && !passwordSetResult.Succeeded)
            {

            }
            return confirmResult.Succeeded ? NoContent() : StatusCode(StatusCodes.Status304NotModified);
        }

        [HttpGet("/user/{email}")]
        [TenantDomainRequired]
        public async Task<IActionResult> Index(string email)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { errors = $"{nameof(email)} cannot be null or empty." });
            }


            var localUserManager = userManager(Domain);
            var user = await localUserManager.FindByEmailAsync(email);
            if (user is null)
            {
                return NotFound();
            }
            var role = await localUserManager.GetRolesAsync(user);
            return Ok(MapUserToUserResponse(user, role.First()));
        }

        [HttpGet("/user/{email}/role")]
        [TenantDomainRequired]
        public async Task<IActionResult> GetUserRole([FromRoute] string email)
        {
            var localUserManager = userManager(Domain);
            var user = await localUserManager.FindByEmailAsync(email);
            if (user is null)
            {
                return NotFound();
            }
            var role = await localUserManager.GetRolesAsync(user);
            return Ok(new { role = role.First() });
        }

        [HttpPut("/user/{username}/password-reset")]
        [TenantDomainRequired]
        public async Task<IActionResult> PutPasswordResetRequest([FromRoute] string username, PasswordResetModel passwordResetModel)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { errors = $"{nameof(username)} cannot be null or empty." });
            }


            var localUserManager = userManager(Domain);


            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(username);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred retrieving the user.");
                return InternalServerError();
            }
            if (user is null)
            {
                return NotFound();
            }

            if (await localUserManager.CheckPasswordAsync(user, passwordResetModel.Password))
            {
                var token = HttpUtility.UrlEncode(await localUserManager.GeneratePasswordResetTokenAsync(user));
                _busPublisher.Send(new SendResetPasswordEmail(user.FirstName, username, Domain, user.Id, localUserManager.TenantOrganizationNameModel.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(username));
                return NoContent();
            }
            var apiErrorContent = new ApiErrorContent();
            apiErrorContent.Errors.Add("The password provided is invalid.");
            return BadRequest(apiErrorContent);
        }

        [HttpGet("/user/all")]
        [TenantDomainRequired]
        public async Task<IActionResult> All()
        {

            var localUserManager = userManager(Domain);
            var users = await localUserManager.Users.OrderBy(_ => _.LastName).ToListAsync();
            if (users is null)
            {
                return NoContent();
            }
            var userResponse = new List<UserResponseModel>();
            foreach (var user in users)
            {
                var role = await localUserManager.GetRolesAsync(user);
                userResponse.Add(MapUserToUserResponse(user, role.First()));
            }
            return Ok(userResponse);
        }

        [NonAction]
        private UserResponseModel MapUserToUserResponse(RangerUser user, string role)
        {
            return new UserResponseModel
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