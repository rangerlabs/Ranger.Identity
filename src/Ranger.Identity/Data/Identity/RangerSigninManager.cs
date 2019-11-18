using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ranger.Common;

namespace Ranger.Identity.Data
{
    public class RangerSignInManager : SignInManager<RangerUser>
    {
        public delegate SignInManager<RangerUser> Factory(ContextTenant contextTenant);

        public RangerSignInManager(ContextTenant contextTenant,
                                   RangerUserManager.Factory userManagerFactory,
                                   IHttpContextAccessor contextAccessor,
                                   IUserClaimsPrincipalFactory<RangerUser> claimsFactory,
                                   IOptions<IdentityOptions> optionsAccessor,
                                   ILogger<SignInManager<RangerUser>> logger,
                                   IAuthenticationSchemeProvider schemes,
                                   IUserConfirmation<RangerUser> confirmation
            ) : base(userManagerFactory.Invoke(contextTenant), contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        { }

        public RangerSignInManager(UserManager<RangerUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<RangerUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<RangerUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<RangerUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        { }
    }
}