using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.RabbitMQ;

namespace Ranger.Identity.Handlers.Commands
{
    public class GeneratePrimaryOwnershipTransferTokenHandler : ICommandHandler<GeneratePrimaryOwnershipTransferToken>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<GeneratePrimaryOwnershipTransferTokenHandler> logger;
        private readonly Func<string, RangerUserManager> userManager;

        public GeneratePrimaryOwnershipTransferTokenHandler(
            IBusPublisher busPublisher,
            ILogger<GeneratePrimaryOwnershipTransferTokenHandler> logger,
            Func<string, RangerUserManager> userManager)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
        }

        public async Task HandleAsync(GeneratePrimaryOwnershipTransferToken message, ICorrelationContext context)
        {
            var localUserManager = userManager(message.Domain);
            var user = await localUserManager.FindByEmailAsync(message.TransferUserEmail);
            if (user is null)
            {
                throw new RangerException($"Failed to find Primary Owner '{message.TransferUserEmail}.'");
            }

            string token = "";
            try
            {
                token = await localUserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PrimaryOwnerTransfer");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate the token to facilitate transfering the Primary Owner role.");
                throw new RangerException("Failed to generate the transfer token.");
            }
            busPublisher.Publish(new PrimaryOwnershipTransferTokenGenerated(token), context);
        }
    }
}