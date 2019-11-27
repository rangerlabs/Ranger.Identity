using System;
using System.Threading.Tasks;
using Ranger.Common;
using Ranger.Identity.Data;

namespace Ranger.Identity
{
    public static class RoleAssignmentValidator
    {
        public static async Task<bool> Validate(RangerUser assignor, RangerUser assignee, RangerUserManager rangerUserManager)
        {
            var assignorRoleEnum = await rangerUserManager.GetRangerRoleAsync(assignor).ConfigureAwait(false);
            var assigneeRoleEnum = await rangerUserManager.GetRangerRoleAsync(assignee).ConfigureAwait(false);


            if (assignorRoleEnum == RolesEnum.TenantOwner)
            {
                return true;
            }

            if (assignorRoleEnum < assigneeRoleEnum)
            {
                return true;
            }

            return false;
        }
    }
}