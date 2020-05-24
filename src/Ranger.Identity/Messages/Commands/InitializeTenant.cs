using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class InitializeTenant : ICommand
    {
        public string TenantId { get; }
        public string DatabasePassword { get; }

        public InitializeTenant(string tenantId, string databasePassword)
        {
            this.TenantId = tenantId;
            this.DatabasePassword = databasePassword;
        }
    }
}