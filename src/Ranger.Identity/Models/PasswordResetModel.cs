using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class PasswordResetModel
    {
        [Required]
        public string Password { get; set; }
    }
}