using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class DeleteUser : ICommand
    {
        public DeleteUser(string tenantId, string email, string commandingUserEmail)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.Email = email;
            this.CommandingUserEmail = commandingUserEmail;

        }
        public string TenantId { get; }
        public string Email { get; }
        public string CommandingUserEmail { get; }


    }
}