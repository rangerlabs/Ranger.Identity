using System.Collections.Generic;
using Ranger.Common;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class CreateUser : ICommand
    {
        public string TenantId { get; set; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public RolesEnum Role { get; }
        public string CommandingUserEmail { get; }
        public IEnumerable<string> AuthorizedProjectIds { get; }

        public CreateUser(string tenantId, string email, string firstName, string lastName, RolesEnum role, string commandingUserEmail, IEnumerable<string> authorizedProjectIds)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(firstName))
            {
                throw new System.ArgumentException($"{nameof(firstName)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(lastName))
            {
                throw new System.ArgumentException($"{nameof(lastName)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Role = role;
            this.CommandingUserEmail = commandingUserEmail;
            this.AuthorizedProjectIds = authorizedProjectIds ?? new List<string>();
        }
    }
}