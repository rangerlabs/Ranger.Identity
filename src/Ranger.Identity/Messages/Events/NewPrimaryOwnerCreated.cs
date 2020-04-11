using Ranger.Identity.Data;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class NewPrimaryOwnerCreated : IEvent
    {
        public NewPrimaryOwnerCreated(string email, string firstName, string lastName, string tenantId, string role)
        {
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.TenantId = tenantId;
            this.Role = role;

        }
        public string Email { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string TenantId { get; }

        public string Role { get; }
    }
}