using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class GeneratePrimaryOwnershipTransferToken : ICommand
    {
        public GeneratePrimaryOwnershipTransferToken(string transferUserEmail,
            string domain)
        {
            if (string.IsNullOrWhiteSpace(transferUserEmail))
            {
                throw new System.ArgumentException($"{nameof(transferUserEmail)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new System.ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            TransferUserEmail = transferUserEmail;
            Domain = domain;
        }

        public string TransferUserEmail { get; }
        public string Domain { get; }
    }
}