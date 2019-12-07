using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespaceAttribute("identity")]
    public class UpdateUserRoleRejected : IRejectedEvent
    {
        public UpdateUserRoleRejected(string reason, string code)
        {
            this.Reason = reason;
            this.Code = code;
        }
        public string Reason { get; set; }
        public string Code { get; set; }
    }
}