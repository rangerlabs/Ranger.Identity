using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Ranger.InternalHttpClient;

namespace Ranger.Identity
{

    public class TenantSubdomainRequiredAttribute : TypeFilterAttribute
    {
        public TenantSubdomainRequiredAttribute() : base(typeof(TenantSubdomainRequiredFilterImpl)) { }
        private class TenantSubdomainRequiredFilterImpl : IAsyncActionFilter
        {
            private readonly ITenantsHttpClient tenantsClient;
            private ILogger<TenantSubdomainRequiredFilterImpl> logger { get; }

            public TenantSubdomainRequiredFilterImpl(ITenantsHttpClient tenantsClient, ILogger<TenantSubdomainRequiredFilterImpl> logger)
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
                        var tenantApiResponse = await tenantsClient.IsConfirmedAsync(domain);
                        if (!tenantApiResponse.IsError)
                        {
                            if (tenantApiResponse.Result)
                            {
                                await next();
                            }
                            else
                            {
                                context.Result = new RedirectResult($"https://{GlobalConfig.IdentityServerOptions.RedirectHost}/enter-domain");
                                // TODO: give info that the domain is not enabled
                                // context.Result = new ForbidResult($"The tenant for the provided subdomain is not enabled '{domain}'. Ensure the domain has been confirmed");
                                return;
                            }
                        }
                        else
                        {
                            context.Result = new RedirectResult($"https://{GlobalConfig.IdentityServerOptions.RedirectHost}/enter-domain");
                            // context.Result = new ForbidResult($"The tenant for the provided subdomain is not enabled '{domain}'. Ensure the domain has been confirmed");
                            return;
                        }

                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"An exception occurred validating whether the domain '{domain}' exists");
                        context.Result = new RedirectResult($"https://{GlobalConfig.IdentityServerOptions.RedirectHost}/enter-domain");
                        return;
                    }
                }
                else
                {
                    this.logger.LogDebug($"No subdomain was found in the request");
                    context.Result = new RedirectResult($"https://{GlobalConfig.IdentityServerOptions.RedirectHost}/enter-domain");
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