using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class PostNewTenantApplicationUserModel
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^([\s\,\.\-\'a-zA-Z]){1,48}$")]
        [StringLength(48, MinimumLength = 1)]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^([\s\,\.\-\'a-zA-Z]){1,48}$")]
        [StringLength(48, MinimumLength = 1)]
        public string LastName { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 8)]
        [RegularExpression(@"^(?!.*\s)(?=.*[`~!@#\$%\^&\*\(\)_\\\+-=\{\}\[\]\|;:'"",<\.>/\?])(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).{8,64}$")]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }
    }
}