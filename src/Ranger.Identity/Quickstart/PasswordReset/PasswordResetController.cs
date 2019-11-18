using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Ranger.Identity;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace IdentityServer4.Quickstart.UI
{
    [SecurityHeaders]
    [AllowAnonymous]
    [TenantSubdomainRequired]
    public class PasswordResetController : Controller
    {
        private readonly UserManager<RangerUser> _userManager;
        private readonly IBusPublisher _busPublisher;
        private readonly ITenantsClient _tenantClient;

        public PasswordResetController(IBusPublisher busPublisher, UserManager<RangerUser> userManager, ITenantsClient tenantClient)
        {
            _tenantClient = tenantClient;
            _busPublisher = busPublisher;
            _userManager = userManager;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet("/PasswordReset")]
        public async Task<IActionResult> PasswordReset()
        {
            return await Task.Run(() => View());
        }

        /// <summary>
        /// Handle postback from password reset
        /// </summary>
        [HttpPost("/PasswordReset")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordReset(PasswordResetViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var (_, domain) = GetDomainFromRequestHost();
                    var tenant = await _tenantClient.GetTenantAsync<TenantOrganizationNameModel>(domain);
                    var token = HttpUtility.UrlEncode(await _userManager.GeneratePasswordResetTokenAsync(user));
                    _busPublisher.Send(new SendResetPasswordEmail(user.FirstName, model.Email, domain, user.Id, tenant.OrganizationName, token), GetContext<SendResetPasswordEmail>(model.Email));
                }
            }
            return View("PasswordResetResult");
        }

        private (int length, string domain) GetDomainFromRequestHost()
        {
            var hostComponents = HttpContext.Request.Host.Host.Split('.');
            return (hostComponents.Length, hostComponents[0]);

        }

        private ICorrelationContext GetContext<T>(string email)
        {
            StringValues domain;
            bool success = HttpContext.Request.Headers.TryGetValue("x-ranger-domain", out domain);

            return CorrelationContext.Create<T>(
                Guid.NewGuid(),
                success ? domain.First() : "",
                email,
                Guid.Empty,
                Request.Path.ToString(),
                HttpContext.TraceIdentifier,
                "",
                this.HttpContext.Connection.Id,
                ""
            );
        }
    }
}