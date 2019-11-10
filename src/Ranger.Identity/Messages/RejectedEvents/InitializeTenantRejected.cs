using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class InitializeTenantRejected : IRejectedEvent
    {
        public string Reason { get; }
        public string Code { get; }

        public InitializeTenantRejected(string reason, string code)
        {

            this.Reason = reason;
            this.Code = code;
        }
    }
}