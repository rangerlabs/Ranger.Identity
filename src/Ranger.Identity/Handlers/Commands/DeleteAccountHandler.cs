using System;
using System.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Ranger.RabbitMQ.BusPublisher;

namespace Ranger.Identity
{
    public class DeleteAccountHandler : ICommandHandler<DeleteAccount>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<CreateUserHandler> logger;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly ISubscriptionsHttpClient subscriptionsHttpClient;
        private readonly IProjectsHttpClient projectsHttpClient;
        private readonly ITenantsHttpClient tenantsHttpClient;

        public DeleteAccountHandler(
            IBusPublisher busPublisher,
            ILogger<CreateUserHandler> logger,
            Func<TenantOrganizationNameModel, RangerUserManager> userManager,
            ISubscriptionsHttpClient subscriptionsHttpClient,
            IProjectsHttpClient projectsHttpClient,
            ITenantsHttpClient tenantsHttpClient)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.subscriptionsHttpClient = subscriptionsHttpClient;
            this.projectsHttpClient = projectsHttpClient;
            this.tenantsHttpClient = tenantsHttpClient;
        }

        public async Task HandleAsync(DeleteAccount command, ICorrelationContext context)
        {
            logger.LogInformation($"Deleting account '{command.Email}' for tenant with domain '{command.TenantId}'");

            var tenantApiResponse = await tenantsHttpClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(tenantApiResponse.Result);
            RangerUser user = null;
            user = await localUserManager.FindByEmailAsync(command.Email);
            var result = await localUserManager.CheckPasswordAsync(user, command.Password);
            if (!result)
            {
                var message = "The password provided was invalid";
                logger.LogDebug(message);
                throw new RangerException(message);
            }

            var deleteResult = await localUserManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                logger.LogError($"Failed to delete account {command.Email}. Errors: {String.Join(';', deleteResult.Errors.Select(_ => _.Description).ToList())}");
                throw new RangerException("Failed to update account");
            }
            busPublisher.Publish(new AccountDeleted(command.TenantId, user.Id, command.Email), context);
        }
    }
}