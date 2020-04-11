using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class InitializeTenant : ICommand
    {
        public string TenantId { get; }
        public string DatabasePassword { get; }

        public InitializeTenant(string TenantId, string databasePassword)
        {
            this.TenantId = TenantId;
            this.DatabasePassword = databasePassword;
        }
    }
}