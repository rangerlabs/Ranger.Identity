using System;
using System.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Common.Data.Exceptions;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    public class UpdateUserPermissionsHandler : ICommandHandler<UpdateUserPermissions>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<UpdateUserPermissionsHandler> logger;
        private readonly Func<string, RangerUserManager> userManager;
        private readonly ITenantsClient tenantsClient;

        public UpdateUserPermissionsHandler(
            IBusPublisher busPublisher,
            ITenantsClient tenantsClient,
            ILogger<UpdateUserPermissionsHandler> logger,
            Func<string, RangerUserManager> userManager)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.tenantsClient = tenantsClient;
        }

        public async Task HandleAsync(UpdateUserPermissions command, ICorrelationContext context)
        {
            logger.LogInformation($"Updating user permissions for '{command.Email}' in domain '{command.Domain}'.");

            try
            {
                var localUserManager = userManager(command.Domain);

                var commandingUser = await localUserManager.FindByEmailAsync(command.CommandingUserEmail);
                var user = await localUserManager.FindByEmailAsync(command.Email);

                var canUpdateUser = await RoleAssignmentValidator.Validate(commandingUser, user, localUserManager);
                if (!canUpdateUser)
                {
                    throw new RangerException("Unauthorized to make changes to the requested user.");
                }

                RolesEnum newRole;
                if (!Enum.TryParse<RolesEnum>(command.Role, true, out newRole))
                {
                    throw new RangerException("The role was not a system role.");
                }

                IdentityResult authorizedProjectsUpdateResult = null;
                if (newRole == RolesEnum.User)
                {
                    var authorizedProjectsList = command.AuthorizedProjects.ToList();
                    if (user.AuthorizedProjects != authorizedProjectsList)
                    {
                        user.AuthorizedProjects = authorizedProjectsList;
                        authorizedProjectsUpdateResult = await localUserManager.UpdateAsync(user).ConfigureAwait(false);
                    }
                    if (!authorizedProjectsUpdateResult.Succeeded)
                    {
                        logger.LogError($"Failed to update users authorized projects. {String.Join(Environment.NewLine, authorizedProjectsUpdateResult.Errors.ToList())}");
                    }
                }

                var currentRole = await localUserManager.GetRangerRoleAsync(user);
                IdentityResult roleAddResult = null;
                if (newRole != currentRole)
                {
                    IdentityResult roleRemoveResult = null;
                    roleAddResult = await localUserManager.AddToRoleAsync(user, command.Role);
                    if (roleAddResult.Succeeded)
                    {
                        roleRemoveResult = await localUserManager.RemoveFromRoleAsync(user, Enum.GetName(typeof(RolesEnum), currentRole));
                        if (!roleRemoveResult.Succeeded)
                        {
                            logger.LogError($"Failed to remove user '{command.Email}' in domain '{command.Domain}' from previous role. Attempting to rolling back the addition of the requested role. {String.Join(Environment.NewLine, roleRemoveResult.Errors.ToList())}");
                            var result = await localUserManager.RemoveFromRoleAsync(user, command.Role);
                            if (result.Succeeded)
                            {
                                logger.LogInformation($"Successfully rolled back additional role '{command.Role}' for '{command.Email}' in domain '{command.Domain}'.");
                            }
                            else
                            {
                                logger.LogError($"Failed to role back additional role '{command.Role}' for '{command.Email}' in domain '{command.Domain}'.");
                            }
                            throw new RangerException("Failed to update user permissions.");
                        }
                    }
                    else
                    {
                        logger.LogError($"Failed to add role '{command.Role}' for '{command.Email}' in domain '{command.Domain}'.");
                        throw new RangerException("Failed to update user permissions.");
                    }
                }

                if (authorizedProjectsUpdateResult is null && roleAddResult is null)
                {
                    throw new RangerException("The user was not modified.");
                }

                busPublisher.Publish(new UserPermissionsUpdated(command.Domain, user.Id, command.Email, user.FirstName, command.Role, command.AuthorizedProjects), context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update user permissions.");
                throw;
            }
        }
    }
}