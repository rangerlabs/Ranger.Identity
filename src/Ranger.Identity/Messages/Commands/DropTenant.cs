using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class DropTenant : ICommand
    {
        public string TenantId { get; }

        public DropTenant(string TenantId)
        {
            this.TenantId = TenantId;
        }
    }
}