using Ranger.Common;

namespace Ranger.Identity
{
    public class TenantOrganizationNameModel : ContextTenant
    {
        public TenantOrganizationNameModel(string databaseUsername, string databasePassword, bool enabled) : base(databaseUsername, databasePassword, enabled)
        { }

        public string OrganizationName { get; set; }
    }
}