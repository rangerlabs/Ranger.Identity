using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{
    public static class Utilities
    {
        public static bool UriMatchesTheHostExcludingSubDomain(string uri)
        {
            Regex validRegex = new Regex($@"^https://(?:.+\.)?{GlobalConfig.IdentityServerOptions.Host}(?::\d{{1,5}})?$");
            return validRegex.IsMatch(uri);
        }

        public static bool RedirectUriMatchesTheHostExcludingSubDomain(string uri)
        {
            Regex validRegex = new Regex($@"^https://(?:.+\.)?{GlobalConfig.IdentityServerOptions.Host}(?::\d{{1,5}})?/callback|silent-refresh\.html$");
            return validRegex.IsMatch(uri);
        }
    }
}