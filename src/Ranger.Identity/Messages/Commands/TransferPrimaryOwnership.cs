using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class TransferPrimaryOwnership : ICommand
    {
        public TransferPrimaryOwnership(string commandingUserEmail,
            string transferUserEmail,
            string domain)
        {
            if (string.IsNullOrWhiteSpace(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(transferUserEmail))
            {
                throw new System.ArgumentException($"{nameof(transferUserEmail)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new System.ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            CommandingUserEmail = commandingUserEmail;
            TransferUserEmail = transferUserEmail;
            Domain = domain;
        }

        public string CommandingUserEmail { get; private set; }
        public string TransferUserEmail { get; private set; }
        public string Domain { get; private set; }
    }
}