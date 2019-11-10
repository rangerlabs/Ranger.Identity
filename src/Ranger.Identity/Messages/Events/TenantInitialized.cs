using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class TenantInitialized : IEvent
    {
        public TenantInitialized() { }
    }
}