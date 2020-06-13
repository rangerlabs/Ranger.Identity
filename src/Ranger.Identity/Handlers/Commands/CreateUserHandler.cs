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

namespace Ranger.Identity
{
    public class CreateUserHandler : ICommandHandler<CreateUser>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<CreateUserHandler> logger;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly SubscriptionsHttpClient subscriptionsHttpClient;
        private readonly ProjectsHttpClient projectsHttpClient;
        private readonly TenantsHttpClient tenantsHttpClient;

        public CreateUserHandler(
            IBusPublisher busPublisher,
            ILogger<CreateUserHandler> logger,
            Func<TenantOrganizationNameModel, RangerUserManager> userManager,
            SubscriptionsHttpClient subscriptionsHttpClient,
            ProjectsHttpClient projectsHttpClient,
            TenantsHttpClient tenantsHttpClient)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.subscriptionsHttpClient = subscriptionsHttpClient;
            this.projectsHttpClient = projectsHttpClient;
            this.tenantsHttpClient = tenantsHttpClient;
        }

        public async Task HandleAsync(CreateUser command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating user '{command.Email}' for tenant with domain '{command.TenantId}'");

            var apiResponse = await tenantsHttpClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(apiResponse.Result);

            var limitsApiResponse = await subscriptionsHttpClient.GetSubscription<SubscriptionLimitDetails>(command.TenantId);
            var projectsApiResult = await projectsHttpClient.GetAllProjects<IEnumerable<ProjectModel>>(command.TenantId);
            var usersCount = await localUserManager.Users.CountAsync();
            if (!limitsApiResponse.Result.Active)
            {
                throw new RangerException("Subscription is inactive");
            }
            if (usersCount >= limitsApiResponse.Result.Limit.Accounts)
            {
                throw new RangerException("Subscription limit met");
            }


            var user = new RangerUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = false,
                FirstName = command.FirstName,
                LastName = command.LastName,
                TenantId = localUserManager.contextTenant.TenantId
            };

            IdentityResult createResult = null;
            IdentityResult roleResult = null;
            try
            {
                createResult = await localUserManager.CreateAsync(user, GlobalConfig.TempPassword);
                roleResult = await localUserManager.AddToRoleAsync(user, command.Role);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create user");
                throw new RangerException($"An unexpected error occurred creating user '{user.Email}'");
            }

            if (!createResult.Succeeded)
            {
                if (createResult.Errors.First().Code == "DuplicateUserName")
                {
                    throw new RangerException("The email address is already in use");
                }
                throw new RangerException($"An unexpected error occurred creating user '{user.Email}'");
            }

            var emailToken = HttpUtility.UrlEncode(await localUserManager.GenerateEmailConfirmationTokenAsync(user));

            busPublisher.Publish(new UserCreated(command.TenantId, user.Id, command.Email, user.FirstName, command.Role, emailToken, command.AuthorizedProjectIds), context);
        }
    }
}