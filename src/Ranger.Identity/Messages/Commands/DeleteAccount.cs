using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class DeleteAccount : ICommand
    {
        public DeleteAccount(string tenantId, string email, string password)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new System.ArgumentException($"{nameof(tenantId)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new System.ArgumentException($"{nameof(email)} was null or whitespace");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new System.ArgumentException($"{nameof(password)} was null or whitespace");
            }

            this.TenantId = tenantId;
            this.Email = email;
            this.Password = password;

        }
        public string TenantId { get; }
        public string Email { get; }
        public string Password { get; }


    }
}