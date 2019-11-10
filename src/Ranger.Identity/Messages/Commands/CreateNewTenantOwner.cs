using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class CreateNewTenantOwner : ICommand
    {
        public CreateNewTenantOwner(string email, string firstName, string lastName, string password, string domain)
        {
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Password = password;
            this.Domain = domain;
        }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Password { get; }
        public string Domain { get; }
    }
}