using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class AccountDeleted : IEvent
    {
        public AccountDeleted(string tenantId, string userId, string email)
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

            this.TenantId = tenantId;
            this.UserId = userId;
            this.Email = email;

        }
        public string TenantId { get; }
        public string UserId { get; }
        public string Email { get; }
    }
}