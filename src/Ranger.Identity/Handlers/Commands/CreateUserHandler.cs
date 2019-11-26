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
    public class CreateUserHandler : ICommandHandler<CreateUser>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<CreateUserHandler> logger;
        private readonly Func<string, RangerUserManager> userManager;
        private readonly ITenantsClient tenantsClient;

        public CreateUserHandler(
            IBusPublisher busPublisher,
            ITenantsClient tenantsClient,
            ILogger<CreateUserHandler> logger,
            Func<string, RangerUserManager> userManager)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.tenantsClient = tenantsClient;
        }

        public async Task HandleAsync(CreateUser command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating user '{command.Email}' for tenant with domain '{command.Domain}'.");

            var localUserManager = userManager(command.Domain);

            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = false,
                FirstName = command.FirstName,
                LastName = command.LastName,
                AuthorizedProjects = command.AuthorizedProjectIds.ToList(),
                database_username = localUserManager.TenantOrganizationNameModel.DatabaseUsername
            };

            IdentityResult createResult = null;
            IdentityResult roleResult = null;
            try
            {
                createResult = await localUserManager.CreateAsync(user);
                roleResult = await localUserManager.AddToRoleAsync(user, command.Role);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create user.");
                throw;
            }

            if (!createResult.Succeeded)
            {
                if (createResult.Errors.First().Code == "DuplicateUserName")
                {
                    throw new RangerException("The email address is already taken.");
                }
                throw new RangerException("Failed to create user.");
            }

            var emailToken = HttpUtility.UrlEncode(await localUserManager.GenerateEmailConfirmationTokenAsync(user));

            busPublisher.Publish(new UserCreated(command.Domain, user.Id, command.Email, user.FirstName, command.Role, emailToken, command.AuthorizedProjectIds), context);
        }
    }
}