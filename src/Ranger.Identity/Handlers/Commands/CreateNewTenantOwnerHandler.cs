using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ranger.Common;
using Ranger.Common.Data.Exceptions;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    public class CreateNewTenantOwnerHandler : ICommandHandler<CreateNewTenantOwner>
    {
        private readonly IBusPublisher busPublisher;
        private readonly Func<string, RangerUserManager> userManager;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger<CreateNewTenantOwnerHandler> logger;

        public CreateNewTenantOwnerHandler(
            IBusPublisher busPublisher,
            Func<string, RangerUserManager> userManager,
            ITenantsClient tenantsClient,
            ILogger<CreateNewTenantOwnerHandler> logger)
        {
            this.busPublisher = busPublisher;
            this.userManager = userManager;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }

        public async Task HandleAsync(CreateNewTenantOwner command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating new tenant owner '{command.Email}' for tenant with domain '{command.Domain}'.");

            var localUserManager = userManager(command.Domain);

            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = true,
                FirstName = command.FirstName,
                LastName = command.LastName,
                database_username = localUserManager.TenantOrganizationNameModel.DatabaseUsername
            };
            try
            {
                await localUserManager.CreateAsync(user, command.Password);
                await localUserManager.AddToRoleAsync(user, "TenantOwner");
            }
            catch (EventStreamDataConstraintException ex)
            {
                logger.LogError(ex, "Falied to create user.");
                throw new RangerException(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create user.");
                throw;
            }

            this.busPublisher.Publish(new NewTenantOwnerCreated(user.Email, user.FirstName, user.LastName, command.Domain, "TenantOwner"), context);
        }
    }
}