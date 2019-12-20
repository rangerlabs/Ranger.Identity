using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class AccountDeleteModel
    {
        [Required]
        public string Password { get; set; }
    }
}