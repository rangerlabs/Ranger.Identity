using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerUser : IdentityUser<string>, IRowLevelSecurityDbSet
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string DatabaseUsername { get; set; }

        public List<string> AuthorizedProjects { get; set; }
    }
}
