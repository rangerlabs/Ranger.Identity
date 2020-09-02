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
using Ranger.RabbitMQ.BusPublisher;

namespace Ranger.Identity
{
    public class CreateNewPrimaryOwnerHandler : ICommandHandler<CreateNewPrimaryOwner>
    {
        private readonly IBusPublisher busPublisher;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly ITenantsHttpClient _tenantsClient;
        private readonly ILogger<CreateNewPrimaryOwnerHandler> logger;

        public CreateNewPrimaryOwnerHandler(
            IBusPublisher busPublisher,
            Func<TenantOrganizationNameModel, RangerUserManager> userManager,
            ILogger<CreateNewPrimaryOwnerHandler> logger,
            ITenantsHttpClient tenantsClient
            )
        {
            this.busPublisher = busPublisher;
            this.userManager = userManager;
            this.logger = logger;
            this._tenantsClient = tenantsClient;
        }

        public async Task HandleAsync(CreateNewPrimaryOwner command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating new tenant owner '{command.Email}' for tenant with domain '{command.TenantId}'");
            var apiResponse = await _tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(apiResponse.Result);

            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = true,
                FirstName = command.FirstName,
                LastName = command.LastName,
                TenantId = localUserManager.contextTenant.TenantId
            };
            try
            {
                await localUserManager.CreateAsync(user, command.Password);
                await localUserManager.AddToRoleAsync(user, "PrimaryOwner");
            }
            catch (EventStreamDataConstraintException ex)
            {
                logger.LogDebug(ex, "Failed to create user {Email}", command.Email);
                throw new RangerException(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred creating user {Email}", command.Email);
                throw new RangerException($"An unexpected error occurred creating user '{command.Email}'");
            }

            this.busPublisher.Publish(new NewPrimaryOwnerCreated(user.Email, user.FirstName, user.LastName, command.TenantId, "PrimaryOwner"), context);
        }
    }
}