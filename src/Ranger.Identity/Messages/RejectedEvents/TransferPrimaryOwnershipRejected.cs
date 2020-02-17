using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class TransferPrimaryOwnershipRejected : IRejectedEvent
    {
        public string Reason { get; }
        public string Code { get; }

        public TransferPrimaryOwnershipRejected(string reason, string code)
        {
            this.Reason = reason;
            this.Code = code;
        }
    }
}