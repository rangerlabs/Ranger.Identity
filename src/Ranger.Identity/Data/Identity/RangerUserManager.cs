using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerUserManager : UserManager<RangerUser>, IDisposable
    {
        public TenantOrganizationNameModel contextTenant { get; }
        public RangerUserManager(
            TenantOrganizationNameModel contextTenant,
            UserStore<RangerUser> userStore,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<RangerUser> passwordHasher,
            IEnumerable<IUserValidator<RangerUser>> userValidators,
            IEnumerable<IPasswordValidator<RangerUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<RangerUser>> logger
        )
            : base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            this.contextTenant = contextTenant;
        }
    }
}