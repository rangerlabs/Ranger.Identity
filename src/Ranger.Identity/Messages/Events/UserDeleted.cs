using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class UserDeleted : IEvent
    {
        public UserDeleted(string tenantId, string userId, string email, string commandingUserEmail)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException($"'{nameof(tenantId)}' cannot be null or whitespace", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new System.ArgumentException($"'{nameof(userId)}' cannot be null or whitespace", nameof(userId));
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new System.ArgumentException($"'{nameof(email)}' cannot be null or whitespace", nameof(userId));
            }

            if (string.IsNullOrEmpty(commandingUserEmail))
            {
                throw new System.ArgumentException($"'{nameof(commandingUserEmail)}' cannot be null or whitespace", nameof(userId));
            }

            this.TenantId = tenantId;
            this.UserId = userId;
            this.Email = email;
            this.CommandingUserEmail = commandingUserEmail;

        }
        public string TenantId { get; }
        public string UserId { get; }
        public string Email { get; }
        public string CommandingUserEmail { get; }


    }
}