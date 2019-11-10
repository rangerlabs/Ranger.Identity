using System;
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
    public class CreateApplicationUserHandler : ICommandHandler<CreateApplicationUser>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<CreateApplicationUserHandler> logger;
        private readonly UserManager<RangerUser> userManager;
        private readonly ITenantsClient tenantsClient;

        public CreateApplicationUserHandler(
            IBusPublisher busPublisher,
            ITenantsClient tenantsClient,
            ILogger<CreateApplicationUserHandler> logger,
            UserManager<RangerUser> userManager)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.tenantsClient = tenantsClient;
        }

        public async Task HandleAsync(CreateApplicationUser command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating user '{command.Email}' for tenant with domain '{command.Domain}'.");
            ContextTenant tenant = null;
            try
            {
                tenant = await this.tenantsClient.GetTenantAsync<ContextTenant>(command.Domain);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An exception occurred retrieving the ContextTenant object. Cannot construct the tenant specific repository.");
                throw;
            }


            var passwordHasher = new PasswordHasher<RangerUser>();
            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = false,
                FirstName = command.FirstName,
                LastName = command.LastName,
                AuthorizedProjects = command.PermittedProjectIds.ToList(),
                DatabaseUsername = tenant.DatabaseUsername
            };

            try
            {
                await userManager.CreateAsync(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falied to create user.");
                throw new RangerException(ex.Message);
            }

            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

            busPublisher.Publish(new NewApplicationUserCreated(command.Domain, command.Email, user.FirstName, command.Role, emailToken, command.PermittedProjectIds), context);
        }
    }
}