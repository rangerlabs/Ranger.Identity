using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Ranger.ApiUtilities;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{

    public class TenantSubdomainRequiredAttribute : TypeFilterAttribute
    {
        public TenantSubdomainRequiredAttribute() : base(typeof(TenantSubdomainRequiredFilterImpl)) { }
        private class TenantSubdomainRequiredFilterImpl : IAsyncActionFilter
        {
            private readonly ITenantsClient tenantsClient;
            private ILogger<TenantSubdomainRequiredFilterImpl> logger { get; }

            public TenantSubdomainRequiredFilterImpl(ITenantsClient tenantsClient, ILogger<TenantSubdomainRequiredFilterImpl> logger)
            {
                this.tenantsClient = tenantsClient;
                this.logger = logger;
            }
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var (length, domain) = GetDomainFromRequestHost(context);
                if (length == 3)
                {
                    try
                    {
                        var tenantApiResponse = await tenantsClient.EnabledAsync<EnabledResult>(domain);
                        if (tenantApiResponse.Enabled)
                        {
                            await next();
                        }
                        else
                        {
                            context.Result = new ForbidResult($"The tenant for the provided subdomain is not enabled '{domain}'. Ensure the domain has been confirmed.");
                            return;
                        }
                    }
                    catch (HttpClientException<EnabledResult> ex)
                    {
                        if ((int)ex.ApiResponse.StatusCode == StatusCodes.Status404NotFound)
                        {
                            context.Result = new RedirectResult($"https://{GlobalConfig.RedirectHost}/enter-domain");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"An exception occurred validating whether the domain '{domain}' exists.");
                        context.Result = new RedirectResult($"https://{GlobalConfig.RedirectHost}/enter-domain");
                        return;
                    }
                }
                else
                {
                    this.logger.LogDebug($"No subdomain was found in the request.");
                    context.Result = new RedirectResult($"https://{GlobalConfig.RedirectHost}/enter-domain");
                    return;
                }

            }
            private (int length, string domain) GetDomainFromRequestHost(ActionExecutingContext context)
            {
                var hostComponents = context.HttpContext.Request.Host.Host.Split('.');
                return (hostComponents.Length, hostComponents[0]);
            }
        }
    }
}