using System.Threading.Tasks;
using Ranger.Common;
using Ranger.Identity.Data;

namespace Ranger.Identity
{
    public static class AssignmentValidator
    {
        public static async Task<bool> ValidateAsync(RangerUser commandingUser, RangerUser recipient, RangerUserManager rangerUserManager)
        {
            var commandingUserRoleEnum = await rangerUserManager.GetRangerRoleAsync(commandingUser);
            var recipientRoleEnum = await rangerUserManager.GetRangerRoleAsync(recipient);

            if (commandingUserRoleEnum == RolesEnum.User)
            {
                return false;
            }
            if (commandingUserRoleEnum == RolesEnum.PrimaryOwner && recipientRoleEnum == RolesEnum.PrimaryOwner)
            {
                return false;
            }
            if (commandingUserRoleEnum == RolesEnum.PrimaryOwner || commandingUserRoleEnum <= recipientRoleEnum)
            {
                return true;
            }
            return false;
        }
    }
}