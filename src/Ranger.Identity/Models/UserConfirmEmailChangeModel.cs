using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class UserConfirmEmailChangeModel
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}