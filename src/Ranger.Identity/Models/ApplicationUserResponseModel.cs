
namespace Ranger.Identity
{
    public class ApplicationUserResponseModel
    {
        public string SubjectId { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Role { get; set; }

        public int Version { get; set; }
    }
}