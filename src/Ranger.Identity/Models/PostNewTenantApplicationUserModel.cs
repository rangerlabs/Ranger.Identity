using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class PostNewTenantApplicationUserModel
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z,.'-]{1}[a-zA-Z ,.'-]{1,46}[a-zA-Z,.'-]{1}$")]
        [StringLength(48, MinimumLength = 1)]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z,.'-]{1}[a-zA-Z ,.'-]{1,46}[a-zA-Z,.'-]{1}$")]
        [StringLength(48, MinimumLength = 1)]
        public string LastName { get; set; }

        [Required]
        [StringLength(124, MinimumLength = 8)]
        [RegularExpression(@"[!@#$%^&*)(+=._-]{1}[a-z]{1}[A-Z]{1}[0-9]{1}")]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }
    }
}