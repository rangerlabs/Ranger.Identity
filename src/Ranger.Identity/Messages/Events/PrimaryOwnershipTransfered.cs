using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    [MessageNamespace("identity")]
    public class PrimaryOwnershipTransfered : IEvent
    { }
}