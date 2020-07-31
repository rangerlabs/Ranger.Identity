using System.Collections.Generic;
using Ranger.Common;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class UserCreated : IEvent
    {
        public string TenantId { get; }
        public string UserId { get; }
        public string Email { get; }
        public string FirstName { get; }
        public RolesEnum Role { get; }
        public string Token { get; }
        public IEnumerable<string> AuthorizedProjects { get; }

        public UserCreated(string tenantId, string userId, string email, string firstName, RolesEnum role, string token, IEnumerable<string> authorizedProjects = null)
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

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new System.ArgumentException($"{nameof(token)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.UserId = userId;
            this.Email = email;
            this.FirstName = firstName;
            this.Role = role;
            this.Token = token;
            this.AuthorizedProjects = authorizedProjects;
        }
    }
}