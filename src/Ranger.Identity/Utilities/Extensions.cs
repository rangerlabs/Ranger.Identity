using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ranger.Common;
using Ranger.Identity.Data;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    public static class Extensions
    {
        /// <summary>
        /// Determines whether the client is configured to use PKCE.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="client_id">The client identifier.</param>
        /// <returns></returns>
        public static async Task<bool> IsPkceClientAsync(this IClientStore store, string client_id)
        {
            if (!string.IsNullOrWhiteSpace(client_id))
            {
                var client = await store.FindEnabledClientByIdAsync(client_id);
                return client?.RequirePkce == true;
            }

            return false;
        }

        public static ICorrelationContext GetCorrelationContextFromHttpContext<T>(this HttpContext context, string domain, string email)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException($"{nameof(domain)} was null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException($"{nameof(email)} was null or whitespace.");
            }

            return CorrelationContext.Create<T>(
                Guid.NewGuid(),
                domain,
                email,
                Guid.Empty,
                context.Request.Path.ToString(),
                context.TraceIdentifier,
                "",
                context.Connection.Id,
                ""
            );
        }

        public static async Task<RolesEnum> GetRangerRoleAsync(this RangerUserManager rangerUserManager, RangerUser rangerUser)
        {
            var roles = await rangerUserManager.GetRolesAsync(rangerUser).ConfigureAwait(false);

            if (roles.Count > 1)
            {
                throw new ArgumentOutOfRangeException($"Assignor '{rangerUser.Email}' is assigned to more than one role.");
            }
            return Enum.Parse<RolesEnum>(roles[0], true);
        }
    }
}
