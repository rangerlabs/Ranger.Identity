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
    public class CreateNewPrimaryOwnerHandler : ICommandHandler<CreateNewPrimaryOwner>
    {
        private readonly IBusPublisher busPublisher;
        private readonly Func<bool, string, RangerUserManager> userManager;
        private readonly ILogger<CreateNewPrimaryOwnerHandler> logger;

        public CreateNewPrimaryOwnerHandler(
            IBusPublisher busPublisher,
            Func<bool, string, RangerUserManager> userManager,
            ILogger<CreateNewPrimaryOwnerHandler> logger)
        {
            this.busPublisher = busPublisher;
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task HandleAsync(CreateNewPrimaryOwner command, ICorrelationContext context)
        {
            logger.LogInformation($"Creating new tenant owner '{command.Email}' for tenant with domain '{command.TenantId}'.");

            var localUserManager = userManager(false, command.TenantId);

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
                logger.LogError(ex, "Falied to create user.");
                throw new RangerException(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create user.");
                throw;
            }

            this.busPublisher.Publish(new NewPrimaryOwnerCreated(user.Email, user.FirstName, user.LastName, command.TenantId, "PrimaryOwner"), context);
        }
    }
}