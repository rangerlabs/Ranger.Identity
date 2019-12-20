using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class DeleteUserModel
    {
        [Required]
        [EmailAddress]
        public string CommandingUserEmail { get; set; }
    }
}