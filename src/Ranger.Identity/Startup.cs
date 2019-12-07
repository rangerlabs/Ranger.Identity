// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


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

namespace Ranger.Identity
{
    public class Startup
    {
        private IWebHostEnvironment Environment;
        private readonly IConfiguration configuration;
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
                options.EnableEndpointRouting = false;

            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            });

            services.AddSingleton<ITenantsClient, TenantsClient>(provider =>
            {
                return new TenantsClient("http://tenants:8082", provider.GetService<ILogger<TenantsClient>>());
            });
            services.AddSingleton<IProjectsClient, ProjectsClient>(provider =>
            {
                return new ProjectsClient("http://projects:8086", provider.GetService<ILogger<ProjectsClient>>());
            });

            services.AddDbContext<RangerIdentityDbContext>(options =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"]);
            });
            services.AddEntityFrameworkNpgsql().AddDbContext<ConfigurationDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"], npgsqlOptions =>
                {
                    var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
                    npgsqlOptions.MigrationsAssembly(migrationsAssembly);
                });
            });
            services.AddEntityFrameworkNpgsql().AddDbContext<PersistedGrantDbContext>((serviceProvider, options) =>
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

            var identityServerBuilder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                if (Environment.IsDevelopment())
                {
                    options.IssuerUri = $"http://localhost.io:5000";
                }
                else
                {
                    options.IssuerUri = $"https://rangerlabs.io";
                }
            })
                .AddAspNetIdentity<RangerUser>()
                .AddRedirectUriValidator<MultitenantRedirectUriValidator>()
                .AddProfileService<ApplicationUserProfileService>()
                // .AddResourceOwnerValidator<ApplicationUserResourceOwnerPasswordValidator>()
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
                .ProtectKeysWithCertificate(new X509Certificate2(configuration["DataProtectionCertPath:Path"]))
                .PersistKeysToDbContext<RangerIdentityDbContext>();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<RangerIdentityDbContext>().AsSelf().InstancePerRequest();
            builder.RegisterType<TenantServiceRangerIdentityDbContext>();
            builder.RegisterInstance<CloudSqlOptions>(configuration.GetOptions<CloudSqlOptions>("cloudSql"));
            builder.RegisterType<RangerIdentityDbContext>().InstancePerDependency();
            builder.Register((c, p) =>
            {
                var options = c.Resolve<IOptions<IdentityOptions>>();
                var passwordHasher = c.Resolve<IPasswordHasher<RangerUser>>();
                var userValidators = c.Resolve<IEnumerable<IUserValidator<RangerUser>>>();
                var passwordValidators = c.Resolve<IEnumerable<IPasswordValidator<RangerUser>>>();
                var keyNormalizer = c.Resolve<ILookupNormalizer>();
                var errors = c.Resolve<IdentityErrorDescriber>();
                var services = c.Resolve<IServiceProvider>();
                var logger = c.Resolve<ILogger<UserManager<RangerUser>>>();


                var provider = c.Resolve<TenantServiceRangerIdentityDbContext>();
                var (dbContextOptions, model) = provider.GetDbContextOptions(p.TypedAs<string>());
                var userStore = new UserStore<RangerUser>(new RangerIdentityDbContext(dbContextOptions));

                return new RangerUserManager(model, userStore, options, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger);
            });
            builder.RegisterType<RangerSignInManager>().As(typeof(SignInManager<RangerUser>));
            builder.AddRabbitMq(this.loggerFactory);
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


            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            app.UsePathBase("/auth");
            InitializeDatabase(app, loggerFactory.CreateLogger<Startup>());
            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
            this.busSubscriber = app.UseRabbitMQ()
                .SubscribeCommand<CreateNewTenantOwner>((c, e) =>
                   new CreateNewTenantOwnerRejected(e.Message, ""))
                .SubscribeCommand<CreateUser>((c, e) =>
                   new CreateUserRejected(e.Message, ""))
                .SubscribeCommand<InitializeTenant>((c, e) =>
                   new InitializeTenantRejected(e.Message, "")
                )
                .SubscribeCommand<UpdateUserRole>((c, e) =>
                    new UpdateUserRoleRejected(e.Message, "")
                );


            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
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
                logger.LogInformation("Adding new Client configurations.");
                foreach (var client in Config.GetClients())
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                logger.LogInformation("Adding new IdentityResource configurations.");
                foreach (var resource in Config.GetIdentityResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                logger.LogInformation("Adding new ApiResource configurations.");
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
                logger.LogInformation("Removeing existing Client configurations.");
                foreach (var client in context.Clients)
                {
                    context.Remove(client);
                }
                context.SaveChanges();
            }
            if (context.IdentityResources.Any())
            {
                logger.LogInformation("Removeing existing IdentityResource configurations.");
                foreach (var identityResource in context.IdentityResources)
                {
                    context.Remove(identityResource);
                }
                context.SaveChanges();
            }
            if (context.ApiResources.Any())
            {
                logger.LogInformation("Removeing existing ApiResource configurations.");
                foreach (var apiResource in context.ApiResources)
                {
                    context.Remove(apiResource);
                }
                context.SaveChanges();
            }
        }

        private void OnShutdown()
        {
            this.busSubscriber.Dispose();
        }
    }
}