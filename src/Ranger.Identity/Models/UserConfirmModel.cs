using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class UserConfirmModel
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [StringLength(64, MinimumLength = 8)]
        [RegularExpression(@"^(?!.*\s)(?=.*[`~!@#\$%\^&\*\(\)_\\\+-=\{\}\[\]\|;:'"",<\.>/\?])(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).{8,64}$")]
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}