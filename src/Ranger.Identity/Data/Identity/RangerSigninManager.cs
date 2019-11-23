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
        public delegate SignInManager<RangerUser> Factory(UserManager<RangerUser> userStore);

        public RangerSignInManager(UserManager<RangerUser> userStore,
                                   IHttpContextAccessor contextAccessor,
                                   IUserClaimsPrincipalFactory<RangerUser> claimsFactory,
                                   IOptions<IdentityOptions> optionsAccessor,
                                   ILogger<SignInManager<RangerUser>> logger,
                                   IAuthenticationSchemeProvider schemes,
                                   IUserConfirmation<RangerUser> confirmation
            ) : base(userStore, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        { }
    }
}