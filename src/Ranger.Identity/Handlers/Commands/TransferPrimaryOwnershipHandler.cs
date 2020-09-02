using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;
using Ranger.RabbitMQ.BusPublisher;

namespace Ranger.Identity.Handlers.Commands
{
    public class TransferPrimaryOwnershipHandler : ICommandHandler<TransferPrimaryOwnership>
    {
        private readonly IBusPublisher busPublisher;
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> userManager;
        private readonly ILogger<TransferPrimaryOwnership> logger;
        private readonly ITenantsHttpClient tenantsHttpClient;

        public TransferPrimaryOwnershipHandler(
            IBusPublisher busPublisher,
            Func<TenantOrganizationNameModel, RangerUserManager> userManager,
            ILogger<TransferPrimaryOwnership> logger,
            ITenantsHttpClient tenantsHttpClient
        )
        {
            this.busPublisher = busPublisher;
            this.userManager = userManager;
            this.logger = logger;
            this.tenantsHttpClient = tenantsHttpClient;
        }

        public async Task HandleAsync(TransferPrimaryOwnership command, ICorrelationContext context)
        {
            logger.LogInformation($"Transfering Primary Ownership of domain '{command.TenantId}' from '{command.CommandingUserEmail}' to '{command.TransferUserEmail}'");
            var apiResponse = await tenantsHttpClient.GetTenantByIdAsync<TenantOrganizationNameModel>(command.TenantId);
            var localUserManager = userManager(apiResponse.Result);
            var commandingUser = await localUserManager.FindByEmailAsync(command.CommandingUserEmail);
            var transferUser = await localUserManager.FindByEmailAsync(command.TransferUserEmail);

            if (commandingUser is null)
            {
                throw new RangerException("The user requesting the transfer was not found");
            }
            if (transferUser is null)
            {
                throw new RangerException("The recipient of the transfer request was not found");
            }

            var tokenResult = await localUserManager.VerifyUserTokenAsync(transferUser, TokenOptions.DefaultProvider, "PrimaryOwnerTransfer", command.Token);
            if (tokenResult)
            {
                var currentCommandingUserRole = await localUserManager.GetRangerRoleAsync(commandingUser);

                if (currentCommandingUserRole == RolesEnum.PrimaryOwner)
                {

                    var toOwnerResult = await localUserManager.AddToRoleAsync(commandingUser, Enum.GetName(typeof(RolesEnum), RolesEnum.Owner));
                    if (toOwnerResult.Succeeded)
                    {
                        var fromPrimaryOwnerRole = await localUserManager.RemoveFromRoleAsync(commandingUser, Enum.GetName(typeof(RolesEnum), RolesEnum.PrimaryOwner));
                        if (fromPrimaryOwnerRole.Succeeded)
                        {
                            var currentRole = await localUserManager.GetRangerRoleAsync(transferUser);
                            var toPrimaryOwnerResult = await localUserManager.AddToRoleAsync(transferUser, Enum.GetName(typeof(RolesEnum), RolesEnum.PrimaryOwner));
                            if (toPrimaryOwnerResult.Succeeded)
                            {
                                var fromOwnerResult = await localUserManager.RemoveFromRoleAsync(transferUser, Enum.GetName(typeof(RolesEnum), currentRole));
                                if (fromOwnerResult.Succeeded)
                                {
                                    busPublisher.Publish(new PrimaryOwnershipTransfered(), context);
                                }
                                else
                                {
                                    logger.LogError($"The Primary Owner role was transfered successfully but failed to remove the previous role from the new Primary Owner, {command.TransferUserEmail}. Verify the user does not have a redundant role");
                                    throw new RangerException("An unspecified error occurred transfering the domain. Please contant Ranger support for additional assitance");
                                }
                            }
                            else
                            {
                                logger.LogWarning("Failed to transfer user to the Primary Owner role. Attempting to revert transfer");
                                var undoToPrimaryOwner = await localUserManager.AddToRoleAsync(commandingUser, Enum.GetName(typeof(RolesEnum), RolesEnum.PrimaryOwner));
                                if (undoToPrimaryOwner.Succeeded)
                                {
                                    var undoFromOwner = await localUserManager.RemoveFromRoleAsync(commandingUser, Enum.GetName(typeof(RolesEnum), RolesEnum.Owner));
                                    if (undoToPrimaryOwner.Succeeded)
                                    {
                                        logger.LogError("Failed to remove un-transfered Primary Owner from additional Owner role. Verify the user does not have a redundant role");
                                        throw new RangerException("Failed transfering ownership of the domain");
                                    }
                                }
                                else
                                {
                                    logger.LogError("Failed to un-transfer former Primary Owner from Owner back to Primary Owner");
                                    throw new RangerException("Failed transfering ownership of the domain");
                                }
                            }
                        }
                        else
                        {
                            logger.LogWarning("Successfully added the Primary Owner to the Owner role but failed to remove the Primary Owner role. Attempting to remove the Owner role");
                            var fromOwnerRole = await localUserManager.RemoveFromRoleAsync(commandingUser, Enum.GetName(typeof(RolesEnum), RolesEnum.Owner));
                            if (fromOwnerRole.Succeeded)
                            {
                                logger.LogWarning("The Primary Owner was succcesfully re-added to the Primary Owner role from the Owner role");
                                throw new RangerException("Failed transfering ownership of the domain");
                            }
                            else
                            {
                                logger.LogError("Failed to add the Primary Owner to the Owner role and could not revert them to the exclusive Primary Owner role");
                                throw new RangerException("Failed transfering ownership of the domain");
                            }
                        }
                    }
                    else
                    {
                        throw new RangerException("Failed to assign the Primary Owner to the Owner role");
                    }
                }
                else
                {
                    throw new RangerException("The user executing the transfer request is not in the Primary Owner role");
                }
            }
            else
            {
                throw new RangerException("The token was invalid for the transfer");
            }
        }
    }
}