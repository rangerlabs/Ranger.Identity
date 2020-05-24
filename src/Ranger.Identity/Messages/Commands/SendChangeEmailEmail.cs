using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("notifications")]
    public class SendChangeEmailEmail : ICommand
    {
        public SendChangeEmailEmail(string firstName, string email, string tenantId, string organization, string token)
        {
            this.FirstName = firstName;
            this.Email = email;
            this.TenantId = tenantId;
            this.Organization = organization;
            this.Token = token;

        }
        public string FirstName { get; }
        public string Email { get; }
        public string TenantId { get; }
        public string Organization { get; }
        public string Token { get; }
    }
}