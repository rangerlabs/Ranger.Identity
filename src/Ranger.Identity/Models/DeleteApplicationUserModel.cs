using System.ComponentModel.DataAnnotations;

namespace Ranger.Identity
{
    public class DeleteApplicationUserModel
    {
        [Required]
        public string SubjectId { get; set; }
    }
}