using System.Collections.Generic;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class UpdateUserRole : ICommand
    {
        public string TenantId { get; }
        public string Email { get; }
        public string CommandingUserEmail { get; }
        public string Role { get; }

        public UpdateUserRole(string tenantId, string email, string commandingUserEmail, string role = "")
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.Email = email;
            this.CommandingUserEmail = commandingUserEmail;
            this.Role = role;
        }
    }
}