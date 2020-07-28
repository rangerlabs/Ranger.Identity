using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Hosting;

namespace Ranger.Identity
{
    public class MultitenantRedirectUriValidator : IRedirectUriValidator
    {
        private readonly string postmanRedirect = "https://oauth.pstmn.io/v1/callback";
        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = Utilities.UriMatchesTheHostExcludingSubDomain(requestedUri);
            return Task.Run(() => { return isAllowed; });
        }

        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = false;
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Production)
            {
                isAllowed = Utilities.RedirectUriMatchesTheHostExcludingSubDomain(requestedUri);
            }
            else
            {
                isAllowed = Utilities.RedirectUriMatchesTheHostExcludingSubDomain(requestedUri) || requestedUri == postmanRedirect;
            }
            return Task.Run(() => { return isAllowed; });
        }
    }
}