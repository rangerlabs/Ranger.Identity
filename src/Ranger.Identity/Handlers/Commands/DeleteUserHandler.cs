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
    public class DeleteUserHandler : ICommandHandler<DeleteUser>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<CreateUserHandler> logger;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly ISubscriptionsHttpClient subscriptionsHttpClient;
        private readonly IProjectsHttpClient projectsHttpClient;
        private readonly ITenantsHttpClient tenantsHttpClient;

        public DeleteUserHandler(
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

        public async Task HandleAsync(DeleteUser command, ICorrelationContext context)
        {
            logger.LogInformation($"Deleting user '{command.Email}' for tenant with domain '{command.TenantId}'");

            var tenantApiResponse = await tenantsHttpClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(tenantApiResponse.Result);
            RangerUser user = null;
            user = await localUserManager.FindByEmailAsync(command.Email);
            if (user is null)
            {
                throw new RangerException("No user was found for the provided email");
            }
            var commandingUser = (await localUserManager.FindByEmailAsync(command.CommandingUserEmail));
            if (!await AssignmentValidator.ValidateAsync(commandingUser, user, localUserManager))
            {
                this.logger.LogWarning("An attempt to delete a user was made from a forbidden commanding user");
                throw new RangerException("You are forbidden from performing this action");
            }
            var deleteResult = await localUserManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                logger.LogError($"Failed to delete user {command.Email}. Errors: {String.Join(';', deleteResult.Errors.Select(_ => _.Description).ToList())}");
                throw new RangerException("Failed to delete user");
            }

            busPublisher.Publish(new UserDeleted(command.TenantId, user.Id, command.Email, command.CommandingUserEmail), context);
        }
    }
}