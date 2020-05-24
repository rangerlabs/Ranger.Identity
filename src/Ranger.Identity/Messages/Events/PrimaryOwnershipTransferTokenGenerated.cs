using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class PrimaryOwnershipTransferTokenGenerated : IEvent
    {
        public string Token { get; set; }
        public PrimaryOwnershipTransferTokenGenerated(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new System.ArgumentException($"{nameof(token)} was null or whitespace");
            }
            Token = token;
        }
    }
}