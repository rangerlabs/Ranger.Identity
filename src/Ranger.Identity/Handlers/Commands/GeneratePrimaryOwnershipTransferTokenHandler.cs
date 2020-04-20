using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Identity.Handlers.Commands
{
    public class GeneratePrimaryOwnershipTransferTokenHandler : ICommandHandler<GeneratePrimaryOwnershipTransferToken>
    {
        private readonly IBusPublisher busPublisher;
        private readonly ILogger<GeneratePrimaryOwnershipTransferTokenHandler> logger;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly TenantsHttpClient tenantsHttpClient;

        public GeneratePrimaryOwnershipTransferTokenHandler(
            IBusPublisher busPublisher,
            ILogger<GeneratePrimaryOwnershipTransferTokenHandler> logger,
            Func<TenantOrganizationNameModel, RangerUserManager> userManager,
            TenantsHttpClient tenantsHttpClient)
        {
            this.busPublisher = busPublisher;
            this.logger = logger;
            this.userManager = userManager;
            this.tenantsHttpClient = tenantsHttpClient;
        }


        public async Task HandleAsync(GeneratePrimaryOwnershipTransferToken command, ICorrelationContext context)
        {
            var apiResponse = await tenantsHttpClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(apiResponse.Result);
            var user = await localUserManager.FindByEmailAsync(command.TransferUserEmail);
            if (user is null)
            {
                throw new RangerException($"Failed to find Primary Owner '{command.TransferUserEmail}.'");
            }

            string token = "";
            try
            {
                token = HttpUtility.UrlEncode(await localUserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PrimaryOwnerTransfer"));
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