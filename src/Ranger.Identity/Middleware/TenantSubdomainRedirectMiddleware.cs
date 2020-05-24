using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;

//This middleware handles requests made be the oidc-client via the signinRedirect() to the /connect/authorize endpoint.
//It pulls the domain out of the acr_values of the returnUrl and redirects to the appropriate subdomain.
namespace Ranger.Identity
{
    public class TenantSubdomainRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantSubdomainRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/connect/authorize")
            {
                if (context.Request.Host.Host.Split('.').Length == 3)
                {
                    var queryParams = QueryHelpers.ParseQuery(context.Request.QueryString.Value);
                    if (queryParams.Count > 0)
                    {
                        var acrValues = queryParams["acr_values"];
                        if (acrValues.Count == 1)
                        {
                            var domain = Regex.Match(acrValues, "tenant:([a-zA-Z0-9]{1}[a-zA-Z0-9-]{1,26}[a-zA-Z0-9]{1}$)").Groups[1].ToString();
                            var redirectString = context.Request.Scheme + "://" + domain + "." + context.Request.Host.Value + context.Request.Path + context.Request.QueryString;
                            context.Response.Redirect(redirectString);
                            return;
                        }
                    }
                }
            }
            await _next(context);
        }
    }
}