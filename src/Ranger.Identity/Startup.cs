using Ranger.Identity.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Autofac;
using Ranger.RabbitMQ;
using Ranger.InternalHttpClient;
using IdentityServer4.EntityFramework.DbContexts;
using System.Reflection;
using Ranger.Common;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.Services;
using System.Linq;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Ranger.ApiUtilities;
using Microsoft.AspNetCore.Mvc.Versioning;
using Ranger.Monitoring.HealthChecks;
using Newtonsoft.Json.Converters;

namespace Ranger.Identity
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private IWebHostEnvironment Environment;
        private ILoggerFactory loggerFactory;
        private IBusSubscriber busSubscriber;

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<OperationCanceledExceptionFilter>();
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            services.AddRangerApiVersioning();
            services.ConfigureAutoWrapperModelStateResponseFactory();
            if (Environment.IsDevelopment())
            {
                services.AddSwaggerGen("Identity API", "v1");
            }

            var identityAuthority = configuration["httpClient:identityAuthority"];
            services.AddPollyPolicyRegistry();
            services.AddTenantsHttpClient("http://tenants:8082", identityAuthority, "tenantsApi", "cKprgh9wYKWcsm");
            services.AddProjectsHttpClient("http://projects:8086", identityAuthority, "projectsApi", "usGwT8Qsp4La2");
            services.AddSubscriptionsHttpClient("http://subscriptions:8089", identityAuthority, "subscriptionsApi", "4T3SXqXaD6GyGHn4RY");

            services.AddDbContext<RangerIdentityDbContext>(options =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"]);
            });
            services.AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"], npgsqlOptions =>
                {
                    var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
                    npgsqlOptions.MigrationsAssembly(migrationsAssembly);
                });
            });
            services.AddDbContext<PersistedGrantDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"], npgsqlOptions =>
                {
                    var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
                    npgsqlOptions.MigrationsAssembly(migrationsAssembly);
                });
            });
            services.AddHttpContextAccessor();

            services.AddTransient<IIdentityDbContextInitializer, RangerIdentityDbContextInitializer>();
            services.AddTransient<IConfigurationDbContextInitializer, ConfigurationDbContextInitializer>();
            services.AddTransient<IPersistedGrantDbContextInitializer, PersistedGrantDbContextInitializer>();
            services.AddTransient<ILoginRoleRepository<RangerIdentityDbContext>, LoginRoleRepository<RangerIdentityDbContext>>();


            services.AddIdentity<RangerUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
                .AddEntityFrameworkStores<RangerIdentityDbContext>()
                .AddDefaultTokenProviders();

            GlobalConfig.IdentityServerOptions = configuration.GetOptions<IdentityServerOptions>("identityServer");
            var identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.IssuerUri = configuration["identityServer:IssuerUri"];
                })
                    .AddAspNetIdentity<RangerUser>()
                    .AddRedirectUriValidator<MultitenantRedirectUriValidator>()
                    .AddProfileService<ApplicationUserProfileService>()
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder =>
                        {
                            builder.UseNpgsql(configuration["cloudSql:ConnectionString"]);
                        };
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = builder =>
                        {
                            builder.UseNpgsql(configuration["cloudSql:ConnectionString"]);
                        };
                        options.EnableTokenCleanup = true;
                        options.TokenCleanupInterval = 60;
                    });

            identityServerBuilder.AddSigningCredential(new X509Certificate2(configuration["IdentitySigningCertPath:Path"]));
            identityServerBuilder.AddValidationKey(new X509Certificate2(configuration["IdentityValidationCertPath:Path"]));

            services.AddLocalApiAuthentication();
            services.AddSingleton<ICorsPolicyService, CorsPolicyService>();

            services.AddDataProtection()
                .SetApplicationName("Identity")
                .ProtectKeysWithCertificate(new X509Certificate2(configuration["DataProtectionCertPath:Path"]))
                .UnprotectKeysWithAnyCertificate(new X509Certificate2(configuration["DataProtectionCertPath:Path"]))
                .PersistKeysToDbContext<RangerIdentityDbContext>();

            services.AddLiveHealthCheck();
            services.AddEntityFrameworkHealthCheck<IdentityDbContext>();
            services.AddDockerImageTagHealthCheck();
            services.AddRabbitMQHealthCheck();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<RangerIdentityDbContext>().AsSelf().InstancePerRequest();
            builder.RegisterType<RangerIdentityDbContext>().AsSelf().InstancePerDependency();
            builder.RegisterType<TenantServiceRangerIdentityDbContext>();
            builder.RegisterInstance<CloudSqlOptions>(configuration.GetOptions<CloudSqlOptions>("cloudSql"));
            builder.Register((c, p) =>
            {
                var provider = c.Resolve<TenantServiceRangerIdentityDbContext>();
                var value = p.TypedAs<TenantOrganizationNameModel>();
                var (dbContextOptions, model) = provider.GetDbContextOptions(value);
                var userStore = new UserStore<RangerUser>(new RangerIdentityDbContext(dbContextOptions));

                return new RangerUserManager(model,
                    userStore,
                    c.Resolve<IOptions<IdentityOptions>>(),
                    c.Resolve<IPasswordHasher<RangerUser>>(),
                    c.Resolve<IEnumerable<IUserValidator<RangerUser>>>(),
                    c.Resolve<IEnumerable<IPasswordValidator<RangerUser>>>(),
                    c.Resolve<ILookupNormalizer>(),
                    c.Resolve<IdentityErrorDescriber>(),
                    c.Resolve<IServiceProvider>(),
                    c.Resolve<ILogger<UserManager<RangerUser>>>());
            }).InstancePerDependency();
            builder.Register((c, p) =>
            {
                return new RangerSignInManager(c.Resolve<RangerUserManager>(p),
                    c.Resolve<IHttpContextAccessor>(),
                    c.Resolve<IUserClaimsPrincipalFactory<RangerUser>>(),
                    c.Resolve<IOptions<IdentityOptions>>(),
                    c.Resolve<ILogger<SignInManager<RangerUser>>>(),
                    c.Resolve<IAuthenticationSchemeProvider>(),
                    c.Resolve<IUserConfirmation<RangerUser>>());
            }).InstancePerDependency();
            builder.AddRabbitMq();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false
            };

            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);

            //TODO: Only initialize once and not in production
            InitializeDatabase(app, loggerFactory.CreateLogger<Startup>());

            app.UseIdentityServer();
            this.busSubscriber = app.UseRabbitMQ(applicationLifetime)
                .SubscribeCommand<CreateNewPrimaryOwner>((c, e) =>
                   new CreateNewPrimaryOwnerRejected(e.Message, ""))
                .SubscribeCommand<CreateUser>((c, e) =>
                   new CreateUserRejected(e.Message, ""))
                .SubscribeCommand<InitializeTenant>((c, e) =>
                   new InitializeTenantRejected(e.Message, ""))
                .SubscribeCommand<UpdateUserRole>((c, e) =>
                    new UpdateUserRoleRejected(e.Message, ""))
                .SubscribeCommand<TransferPrimaryOwnership>((c, e) =>
                    new TransferPrimaryOwnershipRejected(e.Message, ""))
                .SubscribeCommand<GeneratePrimaryOwnershipTransferToken>((c, e) =>
                    new GeneratePrimaryOwnershipTransferTokenRejected(e.Message, ""))
                .SubscribeCommand<DeleteUser>((c, e) =>
                    new DeleteUserRejected(e.Message, ""))
                .SubscribeCommand<DeleteAccount>((c, e) =>
                    new DeleteAccountRejected(e.Message, ""));

            app.UsePathBase("/auth");
            if (Environment.IsDevelopment())
            {
                app.UseSwagger("v1", "Identity API");
            }
            app.UseAutoWrapper(false, "/users");
            app.UseUnhandedExceptionLogger();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHealthChecks();
                endpoints.MapLiveTagHealthCheck();
                endpoints.MapEfCoreTagHealthCheck();
                endpoints.MapDockerImageTagHealthCheck();
                endpoints.MapRabbitMQHealthCheck();
            });
        }

        private void InitializeDatabase(IApplicationBuilder app, ILogger<Startup> logger)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                RemoveExistingConfigurationRecords(context, logger);
                AddNewConfigurationRecords(context, logger);
            }
        }

        private static void AddNewConfigurationRecords(ConfigurationDbContext context, ILogger<Startup> logger)
        {
            if (!context.Clients.Any())
            {
                logger.LogInformation("Adding new Client configurations");
                foreach (var client in Config.GetClients())
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                logger.LogInformation("Adding new IdentityResource configurations");
                foreach (var resource in Config.GetIdentityResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                logger.LogInformation("Adding new ApiResource configurations");
                foreach (var resource in Config.GetApiResources())
                {
                    context.ApiResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }

        private static void RemoveExistingConfigurationRecords(ConfigurationDbContext context, ILogger<Startup> logger)
        {
            if (context.Clients.Any())
            {
                logger.LogInformation("Removeing existing Client configurations");
                foreach (var client in context.Clients)
                {
                    context.Remove(client);
                }
                context.SaveChanges();
            }
            if (context.IdentityResources.Any())
            {
                logger.LogInformation("Removeing existing IdentityResource configurations");
                foreach (var identityResource in context.IdentityResources)
                {
                    context.Remove(identityResource);
                }
                context.SaveChanges();
            }
            if (context.ApiResources.Any())
            {
                logger.LogInformation("Removeing existing ApiResource configurations");
                foreach (var apiResource in context.ApiResources)
                {
                    context.Remove(apiResource);
                }
                context.SaveChanges();
            }
        }
    }
}