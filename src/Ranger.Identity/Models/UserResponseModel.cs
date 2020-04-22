
using System.Collections.Generic;

namespace Ranger.Identity
{
    public class UserResponseModel
    {
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool EmailConfirmed { get; set; }

        public string Role { get; set; }
    }
}