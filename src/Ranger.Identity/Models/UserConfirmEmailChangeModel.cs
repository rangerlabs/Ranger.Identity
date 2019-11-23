using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class UserConfirmEmailChangeModel
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string Email { get; set; }
    }
}