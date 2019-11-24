using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class UserConfirmPasswordResetModel
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [StringLength(124, MinimumLength = 8)]
        // [RegularExpression(@"[!@#$%^&*)(+=._-]{1}[a-z]{1}[A-Z]{1}[0-9]{1}")]
        // TODO: Currently this is not working, need to investigate why - validation is currently only happening at the ApiGateway
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}