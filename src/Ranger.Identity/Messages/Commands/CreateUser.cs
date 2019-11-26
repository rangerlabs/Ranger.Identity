using System.Collections.Generic;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class CreateUser : ICommand
    {
        public string Domain { get; set; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Role { get; }
        public string CommandingUserEmail { get; }
        public IEnumerable<string> AuthorizedProjectIds { get; }

        public CreateUser(string domain, string email, string firstName, string lastName, string role, string commandingUserEmail, IEnumerable<string> authorizedProjectIds)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new System.ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace.");
            }

            if (string.IsNullOrEmpty(firstName))
            {
                throw new System.ArgumentException($"{nameof(firstName)} was null or whitespace.");
            }

            if (string.IsNullOrEmpty(lastName))
            {
                throw new System.ArgumentException($"{nameof(lastName)} was null or whitespace.");
            }

            if (string.IsNullOrEmpty(role))
            {
                throw new System.ArgumentException($"{nameof(role)} was null or whitespace.");
            }

            if (string.IsNullOrEmpty(commandingUserEmail))
            {
                throw new System.ArgumentException($"{nameof(commandingUserEmail)} was null or whitespace.");
            }

            this.Domain = domain;
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Role = role;
            this.CommandingUserEmail = commandingUserEmail;
            this.AuthorizedProjectIds = authorizedProjectIds ?? new List<string>();
        }
    }
}