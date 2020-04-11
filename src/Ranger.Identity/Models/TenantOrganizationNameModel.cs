using Ranger.Common;

namespace Ranger.Identity
{
    public class TenantOrganizationNameModel : ContextTenant
    {
        public TenantOrganizationNameModel(string TenantId, string databasePassword, bool enabled) : base(TenantId, databasePassword, enabled)
        { }

        public string OrganizationName { get; set; }
    }
}