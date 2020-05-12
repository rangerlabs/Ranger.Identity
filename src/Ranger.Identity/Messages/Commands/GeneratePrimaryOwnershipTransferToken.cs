using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class GeneratePrimaryOwnershipTransferToken : ICommand
    {
        public GeneratePrimaryOwnershipTransferToken(string transferUserEmail,
            string tenantId)
        {
            if (string.IsNullOrWhiteSpace(transferUserEmail))
            {
                throw new System.ArgumentException($"{nameof(transferUserEmail)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            TransferUserEmail = transferUserEmail;
            TenantId = tenantId;
        }

        public string TransferUserEmail { get; }
        public string TenantId { get; }
    }
}