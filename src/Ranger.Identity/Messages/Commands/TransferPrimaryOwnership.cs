using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class TransferPrimaryOwnership : ICommand
    {
        public TransferPrimaryOwnership(string commandingUserEmail,
            string transferUserEmail,
            string tenantId,
            string token)
        {
            if (string.IsNullOrWhiteSpace(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(transferUserEmail))
            {
                throw new System.ArgumentException($"{nameof(transferUserEmail)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new System.ArgumentException($"{nameof(token)} was null or whitespace");
            }

            CommandingUserEmail = commandingUserEmail;
            TransferUserEmail = transferUserEmail;
            TenantId = tenantId;
            Token = token;
        }

        public string CommandingUserEmail { get; private set; }
        public string TransferUserEmail { get; private set; }
        public string TenantId { get; private set; }
        public string Token { get; private set; }
    }
}