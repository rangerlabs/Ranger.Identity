using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerUser : IdentityUser<string>
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [StringLength(36)]
        public string TenantId { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string UnconfirmedEmail { get; set; }
    }
}
