using Ranger.Identity.Data;
using Ranger.RabbitMQ;

namespace Ranger.Identity {
    [MessageNamespaceAttribute ("identity")]
    public class NewTenantOwnerCreated : IEvent {
        public NewTenantOwnerCreated (string email, string firstName, string lastName, string tenantDomain, string role) {
            this.Email = email;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.TenantDomain = tenantDomain;
            this.Role = role;

        }
        public string Email { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string TenantDomain { get; }

        public string Role { get; }
    }
}