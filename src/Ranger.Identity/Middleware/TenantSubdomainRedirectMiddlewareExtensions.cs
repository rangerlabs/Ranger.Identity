using Microsoft.AspNetCore.Builder;

namespace Ranger.Identity
{
    public static class TenantSubdomainRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantSubdomainRedirect(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantSubdomainRedirectMiddleware>();
        }
    }
}