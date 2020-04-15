using Ranger.Common;

namespace Ranger.Identity
{
    public class TenantOrganizationNameModel : ContextTenant
    {
        public TenantOrganizationNameModel(string tenantId, string databasePassword, bool enabled) : base(tenantId, databasePassword, enabled)
        { }

        public string OrganizationName { get; set; }
    }
}