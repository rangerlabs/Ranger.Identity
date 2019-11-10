using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Ranger.Identity
{
    public class MultitenantRedirectUriValidator : IRedirectUriValidator
    {
        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = Utilities.UriMatchesTheHostExcludingSubDomain(requestedUri);
            return Task.Run(() => { return isAllowed; });
        }

        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = Utilities.RedirectUriMatchesTheHostExcludingSubDomain(requestedUri);
            return Task.Run(() => { return isAllowed; });
        }
    }
}