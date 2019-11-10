using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerUserManager : UserManager<RangerUser>, IDisposable
    {
        public delegate UserManager<RangerUser> Factory(ContextTenant contextTenant);

        public RangerUserManager(
            IUserStore<RangerUser> userStore,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<RangerUser> passwordHasher,
            IEnumerable<IUserValidator<RangerUser>> userValidators,
            IEnumerable<IPasswordValidator<RangerUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<RangerUser>> logger
        ) : base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        { }

        public RangerUserManager(
            ContextTenant contextTenant,
            RangerUserStore.Factory multitenantApplicationUserStoreFactory,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<RangerUser> passwordHasher,
            IEnumerable<IUserValidator<RangerUser>> userValidators,
            IEnumerable<IPasswordValidator<RangerUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<RangerUser>> logger
        )
            : base(multitenantApplicationUserStoreFactory.Invoke(contextTenant), optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        { }
    }
}