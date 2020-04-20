using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ranger.Common;
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
        private readonly Func<TenantOrganizationNameModel, RangerUserManager> _userManager;
        private readonly IBusPublisher _busPublisher;
        private readonly TenantsHttpClient _tenantsClient;

        public PasswordResetController(IBusPublisher busPublisher, Func<TenantOrganizationNameModel, RangerUserManager> userManager, TenantsHttpClient tenantsClient)
        {
            _tenantsClient = tenantsClient;
            _busPublisher = busPublisher;
            _userManager = userManager;
        }

        /// <summary>
        /// Return password reset view
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
                var tenantApiResponse = await _tenantsClient.GetTenantByDomainAsync<TenantOrganizationNameModel>(Request.Host.GetDomainFromHost());
                var localUserManager = _userManager(tenantApiResponse.Result);
                var user = await localUserManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var (_, domain) = GetDomainFromRequestHost();
                    var apiResponse = await _tenantsClient.GetTenantByIdAsync<TenantOrganizationNameModel>(domain);
                    if (!apiResponse.IsError)
                    {
                        var token = HttpUtility.UrlEncode(await localUserManager.GeneratePasswordResetTokenAsync(user));
                        _busPublisher.Send(new SendResetPasswordEmail(user.FirstName, model.Email, domain, user.Id, apiResponse.Result.OrganizationName, token), HttpContext.GetCorrelationContextFromHttpContext<SendResetPasswordEmail>(domain, model.Email));
                    }
                    ModelState.AddModelError("", "An error occurred resetting the password.");
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