using System.Collections.Generic;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class UserRoleUpdated : IEvent
    {
        public string TenantId { get; }
        public string UserId { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string Role { get; }

        public UserRoleUpdated(string tenantId, string userId, string email, string firstName, string role)
        {

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new System.ArgumentException($"{nameof(userId)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new System.ArgumentException($"{nameof(firstName)} was null or whitespace");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new System.ArgumentException($"{nameof(role)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.UserId = userId;
            this.Email = email;
            this.FirstName = firstName;
            this.Role = role;
        }
    }
}