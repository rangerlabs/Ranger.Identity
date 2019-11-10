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
        private readonly RangerUserManager.Factory multitenantApplicationUserManagerFactory;
        private readonly ITenantsClient tenantsClient;
        private readonly ILogger<CreateNewTenantOwnerHandler> logger;

        public CreateNewTenantOwnerHandler(
            IBusPublisher busPublisher,
            RangerUserManager.Factory multitenantApplicationUserManagerFactory,
            ITenantsClient tenantsClient,
            ILogger<CreateNewTenantOwnerHandler> logger)
        {
            this.busPublisher = busPublisher;
            this.multitenantApplicationUserManagerFactory = multitenantApplicationUserManagerFactory;
            this.tenantsClient = tenantsClient;
            this.logger = logger;
        }

        public async Task HandleAsync(CreateNewTenantOwner command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating new tenant owner '{command.Email}' for tenant with domain '{command.Domain}'.");
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

            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = false,
                FirstName = command.FirstName,
                LastName = command.LastName,
                DatabaseUsername = tenant.DatabaseUsername
            };

            var userManager = multitenantApplicationUserManagerFactory.Invoke(tenant);

            try
            {
                await userManager.CreateAsync(user, command.Password);
                await userManager.AddToRoleAsync(user, "TenantOwner");
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