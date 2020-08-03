using System;
using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Hosting;

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
            var identityApiResource = new ApiResource
            {
                Name = IdentityServerConstants.LocalApi.ScopeName,
                ApiSecrets = {
                new Secret ("89pCcXHuDYTXY".Sha256 ())
                },
                Scopes = { new Scope(IdentityServerConstants.LocalApi.ScopeName, "Identity API") }
            };
            var tenantsApiResource = new ApiResource
            {
                Name = "tenantsApi",
                ApiSecrets = {
                new Secret ("cKprgh9wYKWcsm".Sha256 ())
                },
                Scopes = { new Scope("tenantsApi", "Tenants API") }
            };
            var projectsApiResource = new ApiResource
            {
                Name = "projectsApi",
                ApiSecrets = {
                new Secret ("usGwT8Qsp4La2".Sha256 ())
                },
                Scopes = { new Scope("projectsApi", "Projects API") }
            };
            var integrationsApiResource = new ApiResource
            {
                Name = "integrationsApi",
                ApiSecrets = {
                new Secret ("6HyhzSoSHvxTG".Sha256 ())
                },
                Scopes = { new Scope("integrationsApi", "Integrations API") }
            };
            var geofencesApiResource = new ApiResource
            {
                Name = "geofencesApi",
                ApiSecrets = {
                new Secret ("9pwJgpgpu6PNJi".Sha256 ())
                },
                Scopes = { new Scope("geofencesApi", "Geofences API") }
            };
            var breadcrumbsApiResource = new ApiResource
            {
                Name = "breadcrumbsApi",
                ApiSecrets = {
                new Secret ("Esyz6NkukU98TqzpXU".Sha256 ())
                },
                Scopes = { new Scope("breadcrumbsApi", "Breadcrumbs API") }
            };
            var subscriptionsApiResource = new ApiResource
            {
                Name = "subscriptionsApi",
                ApiSecrets = {
                new Secret ("4T3SXqXaD6GyGHn4RY".Sha256 ())
                },
                Scopes = { new Scope("subscriptionsApi", "Subscriptions API") }
            };

            return new List<ApiResource> {
                apiGatewayResource,
                tenantsApiResource,
                projectsApiResource,
                integrationsApiResource,
                geofencesApiResource,
                subscriptionsApiResource,
                breadcrumbsApiResource,
                identityApiResource
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            var clients = new List<Client>();
            clients.Add(new Client
            {
                ClientId = "TenantsHttpClient",
                ClientName = "TenantsHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("cKprgh9wYKWcsm".Sha256 ()),
                    },
                AllowedScopes = {
                        "tenantsApi",
                    }
            });

            clients.Add(new Client
            {
                ClientId = "IdentityHttpClient",
                ClientName = "IdentityHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("89pCcXHuDYTXY".Sha256 ()),
                    },
                AllowedScopes = {
                        IdentityServerConstants.LocalApi.ScopeName,
                    }
            });
            clients.Add(new Client
            {
                ClientId = "ProjectsHttpClient",
                ClientName = "ProjectsHttpClient",

                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("usGwT8Qsp4La2".Sha256 ()),
                    },
                AllowedScopes = {
                        "projectsApi",
                    }
            });
            clients.Add(new Client
            {
                ClientId = "IntegrationsHttpClient",
                ClientName = "IntegrationsHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("6HyhzSoSHvxTG".Sha256 ()),
                    },
                AllowedScopes = {
                        "integrationsApi",
                    }
            });
            clients.Add(new Client
            {
                ClientId = "GeofencesHttpClient",
                ClientName = "GeofencesHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("9pwJgpgpu6PNJi".Sha256 ()),
                    },
                AllowedScopes = {
                        "geofencesApi",
                    }
            });
            clients.Add(new Client
            {
                ClientId = "BreadcrumbsHttpClient",
                ClientName = "BreadcrumbsHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("Esyz6NkukU98TqzpXU".Sha256 ()),
                    },
                AllowedScopes = {
                        "breadcrumbsApi",
                    }
            });
            clients.Add(new Client
            {
                ClientId = "SubscriptionsHttpClient",
                ClientName = "SubscriptionsHttpClient",
                AccessTokenLifetime = 1800, //30 minutes
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {
                        new Secret ("4T3SXqXaD6GyGHn4RY".Sha256 ())
                    },
                AllowedScopes = {
                        "subscriptionsApi"
                    }
            });
            clients.Add(new Client
            {
                ClientId = "react",
                ClientName = "ReactClient",
                AllowedGrantTypes = GrantTypes.Code,
                AccessTokenLifetime = 900, //15 minutes
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { String.Empty },
                PostLogoutRedirectUris = { $"https://{GlobalConfig.IdentityServerOptions.RedirectHost}" },
                RequireConsent = false,

                AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "apiGateway"
                    },
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != Environments.Production)
            {
                clients.Add(new Client
                {
                    ClientId = "postman",
                    ClientName = "PostmanClient",
                    AccessTokenLifetime = 900, //15 minutes
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    EnableLocalLogin = true,
                    RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                    PostLogoutRedirectUris = { "https://www.postman.com" },
                    AllowedCorsOrigins = { "https://www.postman.com" },

                    ClientSecrets = {
                            new Secret ("cKprgh9wYKWcsm".Sha256 ()),
                            new Secret ("89pCcXHuDYTXY".Sha256 ()),
                            new Secret ("usGwT8Qsp4La2".Sha256 ()),
                            new Secret ("6HyhzSoSHvxTG".Sha256 ()),
                            new Secret ("9pwJgpgpu6PNJi".Sha256 ()),
                            new Secret ("Esyz6NkukU98TqzpXU".Sha256 ()),
                            new Secret ("4T3SXqXaD6GyGHn4RY".Sha256 ())
                        },

                    AllowedScopes = {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.LocalApi.ScopeName,
                            IdentityServerConstants.StandardScopes.Profile,
                            "apiGateway",
                            "tenantsApi",
                            "projectsApi",
                            "integrationsApi",
                            "geofencesApi",
                            "breadcrumbsApi",
                            "subscriptionsApi"
                        }
                });
            }

            return clients;
        }
    }
}