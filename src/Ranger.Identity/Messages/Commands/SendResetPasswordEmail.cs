using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("notifications")]
    public class SendResetPasswordEmail : ICommand
    {
        public SendResetPasswordEmail(string firstName, string email, string domain, string userId, string organizationName, string token)
        {
            this.FirstName = firstName;
            this.UserId = userId;
            this.Email = email;
            this.Domain = domain;
            this.OrganizationName = organizationName;
            this.Token = token;

        }
        public string FirstName { get; }
        public string UserId { get; }
        public string Email { get; }
        public string Domain { get; }
        public string OrganizationName { get; }
        public string Token { get; }
    }
}