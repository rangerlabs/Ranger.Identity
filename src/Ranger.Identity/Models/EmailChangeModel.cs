using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class EmailChangeModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}