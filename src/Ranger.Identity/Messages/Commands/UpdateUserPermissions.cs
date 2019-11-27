using System.Collections.Generic;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class UpdateUserPermissions : ICommand
    {
        public string Domain { get; }
        public string Email { get; }
        public string CommandingUserEmail { get; }
        public string Role { get; }
        public IEnumerable<string> AuthorizedProjects { get; }

        public UpdateUserPermissions(string domain, string email, string commandingUserEmail, string role = "", IEnumerable<string> authorizedProjects = null)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new System.ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace.");
            }

            this.Domain = domain;
            this.Email = email;
            this.CommandingUserEmail = commandingUserEmail;
            this.Role = role;
            this.AuthorizedProjects = authorizedProjects;
        }
    }
}