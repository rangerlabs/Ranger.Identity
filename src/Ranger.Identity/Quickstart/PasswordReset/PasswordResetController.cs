using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Ranger.Identity;
using Ranger.Identity.Data;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace IdentityServer4.Quickstart.UI
{
    [SecurityHeaders]
    [AllowAnonymous]
    [TenantSubdomainRequired]
    public class PasswordResetController : BaseMvcController
    {
        private readonly Func<string, RangerUserManager> _userManager;
        private readonly IBusPublisher _busPublisher;
        private readonly ITenantsClient _tenantClient;

        public PasswordResetController(IBusPublisher busPublisher, Func<string, RangerUserManager> userManager, ITenantsClient tenantClient)
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
                var localUserManager = _userManager(Domain);
                var user = await localUserManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var (_, domain) = GetDomainFromRequestHost();
                    var tenant = await _tenantClient.GetTenantAsync<TenantOrganizationNameModel>(domain);
                    var token = HttpUtility.UrlEncode(await localUserManager.GeneratePasswordResetTokenAsync(user));
                    _busPublisher.Send(new SendResetPasswordEmail(user.FirstName, model.Email, domain, user.Id, tenant.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(model.Email));
                }
            }
            return View("PasswordResetResult");
        }

        private (int length, string domain) GetDomainFromRequestHost()
        {
            var hostComponents = HttpContext.Request.Host.Host.Split('.');
            return (hostComponents.Length, hostComponents[0]);
        }
    }
}