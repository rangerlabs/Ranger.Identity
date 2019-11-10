using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Ranger.Identity
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly ILogger<CorsPolicyService> logger;

        public CorsPolicyService(ILogger<CorsPolicyService> logger)
        {
            this.logger = logger;
        }
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            var isAllowed = Utilities.UriMatchesTheHostExcludingSubDomain(origin);
            return Task.Run(() => { return isAllowed; });
        }
    }
}