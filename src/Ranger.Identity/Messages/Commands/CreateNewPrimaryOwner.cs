using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class CreateNewPrimaryOwner : ICommand
    {
        public CreateNewPrimaryOwner(string email, string firstName, string lastName, string password, string tenantId)
        {
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Password = password;
            this.TenantId = tenantId;
        }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Password { get; }
        public string TenantId { get; }
    }
}