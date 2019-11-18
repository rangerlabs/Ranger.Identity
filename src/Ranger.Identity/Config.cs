using System;
using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4;
using IdentityServer4.Models;

namespace Ranger.Identity
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource> {
                new IdentityResources.OpenId (),
                new IdentityResources.Profile (),
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            var apiGatewayResource = new ApiResource
            {
                Name = "apiGateway",
                Scopes = { new Scope("apiGateway", "API Gateway") }
            };
            var tenantsApiResource = new ApiResource
            {
                Name = "tenantsApi",
                ApiSecrets = {
                    new Secret ("cKprgh9wYKWcsm".Sha256 ())
                },
                Scopes = { new Scope("tenantsApi", "Tenants Api") }
            };
            var projectsApiResource = new ApiResource
            {
                Name = "projectsApi",
                ApiSecrets = {
                    new Secret ("usGwT8Qsp4La2".Sha256())
                },
                Scopes = { new Scope("projectsApi", "Projects Api") }
            };
            var integrationsApiResource = new ApiResource
            {
                Name = "integrationsApi",
                ApiSecrets = {
                    new Secret ("6HyhzSoSHvxTG".Sha256())
                },
                Scopes = { new Scope("integrationsApi", "Integrations Api") }
            };
            var geofencesApiResource = new ApiResource
            {
                Name = "geofencesApi",
                ApiSecrets = {
                    new Secret ("9pwJgpgpu6PNJi".Sha256())
                },
                Scopes = { new Scope("geofencesApi", "Geofences Api") }
            };
            var identityApiResource = new ApiResource
            {
                Name = IdentityServerConstants.LocalApi.ScopeName,
                ApiSecrets = {
                    new Secret ("89pCcXHuDYTXY".Sha256 ())
                },
                Scopes = { new Scope(IdentityServerConstants.LocalApi.ScopeName, "Identity Api") }
            };
            return new List<ApiResource> { apiGatewayResource, tenantsApiResource, projectsApiResource, integrationsApiResource, identityApiResource };
        }

        public static IEnumerable<Client> GetClients()
        {
            var internalApiClient = new Client
            {
                ClientId = "internal",
                ClientName = "InternalClient",

                AccessTokenLifetime = 1800,
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                ClientSecrets = {
                    new Secret ("cKprgh9wYKWcsm".Sha256 ()),
                    new Secret ("89pCcXHuDYTXY".Sha256 ()),
                    new Secret ("usGwT8Qsp4La2".Sha256()),
                    new Secret ("6HyhzSoSHvxTG".Sha256()),
                    new Secret ("9pwJgpgpu6PNJi".Sha256())
                },

                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.LocalApi.ScopeName,
                    "tenantsApi",
                    "projectsApi",
                    "integrationsApi",
                    "geofencesApi"
                }
            };

            var reactClient = new Client()
            {
                ClientId = "react",
                ClientName = "ReactClient",

                AllowedGrantTypes = GrantTypes.Code,
                AccessTokenLifetime = 900, //15 minutes
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { String.Empty },
                PostLogoutRedirectUris = { $"https://{GlobalConfig.RedirectHost}" },
                // AllowAccessTokensViaBrowser = true,
                // AllowOfflineAccess = true,

                RequireConsent = false,

                AllowedScopes = {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "apiGateway"
                },
            };

            return new List<Client>() { reactClient, internalApiClient };
        }
    }
}