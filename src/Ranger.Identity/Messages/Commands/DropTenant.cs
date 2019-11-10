using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class DropTenant : ICommand
    {
        public string DatabaseUsername { get; }

        public DropTenant(string databaseUsername)
        {
            this.DatabaseUsername = databaseUsername;
        }
    }
}