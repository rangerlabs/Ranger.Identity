using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class AccountUpdateModel
    {
        [Required]
        [RegularExpression(@"^([\s\,\.\-\'a-zA-Z]){1,48}$")]
        [StringLength(48, MinimumLength = 1)]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^([\s\,\.\-\'a-zA-Z]){1,48}$")]
        [StringLength(48, MinimumLength = 1)]
        public string LastName { get; set; }
    }
}