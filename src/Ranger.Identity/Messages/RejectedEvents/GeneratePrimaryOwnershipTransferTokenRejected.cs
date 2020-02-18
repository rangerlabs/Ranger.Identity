using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class GeneratePrimaryOwnershipTransferTokenRejected : IRejectedEvent
    {
        public string Reason { get; }
        public string Code { get; }

        public GeneratePrimaryOwnershipTransferTokenRejected(string reason, string code)
        {
            this.Reason = reason;
            this.Code = code;
        }
    }
}