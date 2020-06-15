using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AutoWrapper.Wrappers;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(IdentityServerConstants.LocalApi.PolicyName)]
    public class UsersController : ControllerBase
    {
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly SubscriptionsHttpClient subscriptionsClient;
        private readonly SignInManager<RangerUser> signInManager;
        private readonly IBusPublisher _busPublisher;
        private readonly TenantsHttpClient tenantsClient;
        private readonly ILogger<UsersController> logger;

        public UsersController(
                IBusPublisher busPublisher,
                Func<TenantOrganizationNameModel, RangerUserManager> userManager,
                SubscriptionsHttpClient subscriptionsClient,
                SignInManager<RangerUser> signInManager,
                TenantsHttpClient tenantsClient,
                ILogger<UsersController> logger
            )
        {
            this._busPublisher = busPublisher;
            this.userManager = userManager;
            this.subscriptionsClient = subscriptionsClient;
            this.signInManager = signInManager;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }


        ///<summary>
        /// Gets all users for a tenant
        ///</summary>
        ///<param name="tenantId">The tenant id to retrieve users for</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("/users/{tenantId}")]
        public async Task<ApiResponse> GetUsers(string tenantId)
        {
            try
            {
                var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
                var localUserManager = userManager(apiResponse.Result);
                var users = await localUserManager.Users.OrderBy(_ => _.LastName).ToListAsync();
                var userResponse = new List<UserResponseModel>();
                foreach (var user in users)
                {
                    var role = await localUserManager.GetRolesAsync(user);
                    userResponse.Add(MapUserToUserResponse(user, role.First()));
                }
                return new ApiResponse("Successfully retrieved users", userResponse);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve a user role");
                throw new ApiException("Failed to get user role", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        ///<summary>
        /// Updates a user or account
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to update</param>
        ///<param name="accountInfoModel">The model necessary to update the account</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("/users/{tenantId}/{email}")]
        public async Task<ApiResponse> UserAndAccountUpdate(string tenantId, string email, AccountUpdateModel accountInfoModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve user");
                throw new ApiException("Failed to update account", statusCode: StatusCodes.Status500InternalServerError);
            }
            if (user is null)
            {

                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            user.LastName = accountInfoModel.LastName;
            user.FirstName = accountInfoModel.FirstName;
            var result = await localUserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                logger.LogError($"Failed to update user {email}. Errors: {String.Join(';', result.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to update account", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Successfully updated user account");
        }

        ///<summary>
        /// Gets a user
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to delete</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("/users/{tenantId}/{email}")]
        public async Task<ApiResponse> GetUser(string tenantId, string email)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            var user = await localUserManager.FindByEmailAsync(email);
            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            var role = await localUserManager.GetRolesAsync(user);
            return new ApiResponse("Successfully retrieved user", MapUserToUserResponse(user, role.First()));
        }

        ///<summary>
        /// Gets a user's role
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to delete</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("/users/{tenantId}/{email}/role")]
        public async Task<ApiResponse> GetUserRole(string tenantId, string email)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            var user = await localUserManager.FindByEmailAsync(email);
            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            var role = await localUserManager.GetRolesAsync(user);
            return new ApiResponse("Success retrieved user role", role.First());
        }

        ///<summary>
        /// Sets a user's new password
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user whose password should be changed</param>
        ///<param name="userConfirmPasswordResetModel">The model necessary to change the user's password</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPost("/users/{tenantId}/{email}/password-reset")]
        public async Task<ApiResponse> SetNewPassword(string tenantId, string email, UserConfirmPasswordResetModel userConfirmPasswordResetModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);

            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByIdAsync(email);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve users");
                throw new ApiException("Failed to set password", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }

            var resetResult = await localUserManager.ResetPasswordAsync(user, userConfirmPasswordResetModel.Token, userConfirmPasswordResetModel.NewPassword);
            if (!resetResult.Succeeded)
            {
                logger.LogError($"Failed to set password for user {email}. Errors: {String.Join(';', resetResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to set new password", StatusCodes.Status400BadRequest);
            }

            //TODO: Assess Security implications of a user resetting their password before being confirmed
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var updateResult = await localUserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    logger.LogError($"Failed to confirm user {email} after setting password. Errors: {String.Join(';', resetResult.Errors.Select(_ => _.Description).ToList())}");
                    throw new ApiException("Failed to confirm user", statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            var stampResult = await localUserManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                logger.LogError($"Failed to reset security stamp for user {email}. Errors: {String.Join(';', resetResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to update security stamp", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Successfully set new password");
        }

        ///<summary>
        /// Sets a user's new email
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="userConfirmEmailChangeModel">The model necessary to change the user's email</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [HttpPost("/users/{tenantId}/email-change")]
        public async Task<ApiResponse> SetNewEmail(string tenantId, UserConfirmEmailChangeModel userConfirmEmailChangeModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            RangerUser user = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(userConfirmEmailChangeModel.Email);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve users");
                throw new ApiException("Failed to set password", statusCode: StatusCodes.Status500InternalServerError);
            }

            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }

            var changeResult = await localUserManager.ChangeEmailAsync(user, user.UnconfirmedEmail, userConfirmEmailChangeModel.Token);

            if (!changeResult.Succeeded)
            {
                var message = "Ensure the provided current email and token are correct";
                logger.LogError(message);
                throw new ApiException(message, StatusCodes.Status400BadRequest);
            }

            user.UserName = user.UnconfirmedEmail;
            user.UnconfirmedEmail = "";
            try
            {
                var updateResult = await localUserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    logger.LogError($"Failed to confirm user {userConfirmEmailChangeModel.Email}. Errors: {String.Join(';', updateResult.Errors.Select(_ => _.Description).ToList())}");
                    throw new ApiException("Failed to confirm user", statusCode: StatusCodes.Status500InternalServerError);
                }
            }
            //TODO: Can this be determined from changeResult?
            catch (DbUpdateException ex)
            {
                var postgresException = ex.InnerException as PostgresException;
                if (postgresException.SqlState == "23505")
                {
                    var message = "The requested email is already in use";
                    throw new ApiException(message, StatusCodes.Status409Conflict);
                }
                throw new ApiException("Failed to update user email", statusCode: StatusCodes.Status500InternalServerError);
            }

            var stampResult = await localUserManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                logger.LogError($"Failed to reset security stamp for user {userConfirmEmailChangeModel.Email}. Errors: {String.Join(';', stampResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to update security stamp", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Success set new email");
        }


        ///<summary>
        /// Initiates an email change request
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user who is requesting an email change</param>
        ///<param name="emailChangeModel">The model necessary to change the user's email</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [HttpPut("/users/{tenantId}/{email}/email-change")]
        public async Task<ApiResponse> PutEmailChangeRequest(string tenantId, string email, EmailChangeModel emailChangeModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            RangerUser user = null;
            RangerUser conflictingUser = null;
            try
            {
                user = await localUserManager.FindByEmailAsync(email);
                conflictingUser = await localUserManager.FindByEmailAsync(emailChangeModel.Email);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve users");
                throw new ApiException("Failed to request email change", statusCode: StatusCodes.Status500InternalServerError);
            }
            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            if (conflictingUser != null)
            {
                var message = "The requested email address is unavailable";
                logger.LogDebug(message);
                throw new ApiException(message, StatusCodes.Status409Conflict);
            }

            user.UnconfirmedEmail = emailChangeModel.Email;
            var updateResult = await localUserManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogError($"Failed to request email change for user {email}. Errors: {String.Join(';', updateResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to request email change", statusCode: StatusCodes.Status500InternalServerError);
            }
            var token = HttpUtility.UrlEncode(await localUserManager.GenerateChangeEmailTokenAsync(user, emailChangeModel.Email));
            _busPublisher.Send(new SendChangeEmailEmail(user.FirstName, emailChangeModel.Email, tenantId, localUserManager.contextTenant.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(tenantId, email));
            return new ApiResponse("Successfully requested email change");
        }


        ///<summary>
        /// Confirms a new user
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to confirm</param>
        ///<param name="userConfirmModel">The model necessary to confirm the user</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("/users/{tenantId}/{email}/confirm")]
        public async Task<ApiResponse> ConfirmNewUser(string tenantId, string email, UserConfirmModel userConfirmModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            var user = await localUserManager.FindByIdAsync(email);
            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }

            var confirmResult = await localUserManager.ConfirmEmailAsync(user, userConfirmModel.Token);
            if (!confirmResult.Succeeded)
            {
                var message = "Failed to confirm the user";
                logger.LogError($"{message} Errors: {String.Join(';', confirmResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException(message, StatusCodes.Status400BadRequest);
            }

            var passwordSetResult = await localUserManager.ChangePasswordAsync(user, GlobalConfig.TempPassword, userConfirmModel.NewPassword);
            if (!passwordSetResult.Succeeded)
            {
                logger.LogError($"Failed to set password for user '{user.Email}' after confirming their account. Errors: {String.Join(';', passwordSetResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("The email address was confirmed, but failed to set password", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Successfully confirmed new user", true);
        }

        ///<summary>
        /// Initiates an email change request
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user who is requesting a password reset</param>
        ///<param name="passwordResetModel">The model necessary to reset the user's password</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("/users/{tenantId}/{email}/password-reset")]
        public async Task<ApiResponse> PutPasswordResetRequest(string tenantId, string email, PasswordResetModel passwordResetModel)
        {
            var apiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(apiResponse.Result);
            var user = await localUserManager.FindByEmailAsync(email);
            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            if (await localUserManager.CheckPasswordAsync(user, passwordResetModel.Password))
            {
                var token = HttpUtility.UrlEncode(await localUserManager.GeneratePasswordResetTokenAsync(user));
                _busPublisher.Send(new SendResetPasswordEmail(user.FirstName, email, tenantId, user.Id, localUserManager.contextTenant.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(tenantId, email));
                return new ApiResponse("Successfully set new password", true);
            }
            else
            {
                var message = "The password provided was invalid";
                logger.LogDebug(message);
                throw new ApiException(message, StatusCodes.Status400BadRequest);
            }
        }

        ///<summary>
        /// Deletes a user's account
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to delete</param>
        ///<param name="accountDeleteModel">The model necessary to delete the account</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("/users/{tenantId}/{email}/account")]
        public async Task<ApiResponse> DeleteAccount(string tenantId, [FromRoute] string email, AccountDeleteModel accountDeleteModel)
        {
            var tenantApiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(tenantApiResponse.Result);
            RangerUser user = null;
            user = await localUserManager.FindByEmailAsync(email);
            var result = await localUserManager.CheckPasswordAsync(user, accountDeleteModel.Password);
            if (!result)
            {
                var message = "The password provided was invalid";
                logger.LogDebug(message);
                throw new ApiException(message, StatusCodes.Status400BadRequest);
            }

            var deleteResult = await localUserManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                logger.LogError($"Failed to delete account {email}. Errors: {String.Join(';', deleteResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to update account", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Success deleted account");
        }

        ///<summary>
        /// Deletes a user by email address
        ///</summary>
        ///<param name="tenantId">The tenant id the user is associated with</param>
        ///<param name="email">The email of the user to delete</param>
        ///<param name="deleteUserModel">The model necessary to delete the user</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("/users/{tenantId}/{email}")]
        public async Task<ApiResponse> DeleteUserByEmail(string tenantId, string email, DeleteUserModel deleteUserModel)
        {
            var tenantApiResponse = await tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(tenantId);
            var localUserManager = userManager(tenantApiResponse.Result);
            RangerUser user = null;
            user = await localUserManager.FindByEmailAsync(email);
            var commandingUser = (await localUserManager.FindByEmailAsync(deleteUserModel.CommandingUserEmail));
            if (!await AssignmentValidator.ValidateAsync(commandingUser, user, localUserManager))
            {
                this.logger.LogWarning("An attempt to delete a user was made from a forbidden commanding user");
                throw new ApiException("You are forbidden from performing this action", StatusCodes.Status403Forbidden);
            }

            if (user is null)
            {
                throw new ApiException("No user was found for the provided email", StatusCodes.Status404NotFound);
            }
            var deleteResult = await localUserManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                logger.LogError($"Failed to delete user {email}. Errors: {String.Join(';', deleteResult.Errors.Select(_ => _.Description).ToList())}");
                throw new ApiException("Failed to delete user", statusCode: StatusCodes.Status500InternalServerError);
            }
            return new ApiResponse("Successfully deleted user");
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