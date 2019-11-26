using System.Collections.Generic;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class UserPermissionsUpdated : IEvent
    {
        public string Domain { get; }
        public string UserId { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string Role { get; }
        public IEnumerable<string> AuthorizedProjects { get; }

        public UserPermissionsUpdated(string domain, string userId, string email, string firstName, string role, IEnumerable<string> authorizedProjects = null)
        {

            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new System.ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new System.ArgumentException($"{nameof(userId)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new System.ArgumentException($"{nameof(firstName)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new System.ArgumentException($"{nameof(role)} was null or whitespace.");
            }

            this.Domain = domain;
            this.UserId = userId;
            this.Email = email;
            this.FirstName = firstName;
            this.Role = role;
            this.AuthorizedProjects = authorizedProjects;
        }
    }
}