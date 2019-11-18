using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class UserConfirmModel
    {
        [Required]
        public string RegistrationKey { get; set; }
    }
}