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

namespace Ranger.Identity
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
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
            });

            services.AddSingleton<ITenantsClient, TenantsClient>(provider =>
            {
                return new TenantsClient("http://tenants:8082", provider.GetService<ILogger<TenantsClient>>());
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
            },
                ServiceLifetime.Transient
            );
            services.AddEntityFrameworkNpgsql().AddDbContext<PersistedGrantDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(configuration["cloudSql:ConnectionString"], npgsqlOptions =>
                {
                    var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
                    npgsqlOptions.MigrationsAssembly(migrationsAssembly);
                });
            },
                ServiceLifetime.Transient
            );
            services.AddHttpContextAccessor();

            services.AddTransient<IIdentityDbContextInitializer, IdentityDbContextInitializer>();
            services.AddTransient<IConfigurationDbContextInitializer, ConfigurationDbContextInitializer>();
            services.AddTransient<IPersistedGrantDbContextInitializer, PersistedGrantDbContextInitializer>();
            services.AddTransient<ILoginRoleRepository<RangerIdentityDbContext>, LoginRoleRepository<RangerIdentityDbContext>>();


            services.AddIdentity<RangerUser, IdentityRole>()
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

            if (Environment.IsDevelopment())
            {
                identityServerBuilder.AddDeveloperSigningCredential();
            }
            else
            {
                identityServerBuilder.AddSigningCredential(new X509Certificate2(configuration["IdentitySigningCertPath:Path"]));
                identityServerBuilder.AddValidationKey(new X509Certificate2(configuration["IdentityValidationCertPath:Path"]));
            }

            services.AddLocalApiAuthentication();
            services.AddSingleton<ICorsPolicyService, CorsPolicyService>();

            services.AddDataProtection()
                .ProtectKeysWithCertificate(new X509Certificate2(configuration["DataProtectionCertPath:Path"]))
                .PersistKeysToDbContext<RangerIdentityDbContext>();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<RangerIdentityDbContext>().AsSelf().InstancePerRequest();
            builder.RegisterInstance<CloudSqlOptions>(configuration.GetOptions<CloudSqlOptions>("cloudSql"));
            builder.RegisterType<RangerIdentityDbContext>().InstancePerDependency();
            builder.RegisterType<RangerUserManager>().As(typeof(UserManager<RangerUser>));
            builder.RegisterType<RangerUserStore>().As(typeof(IUserStore<RangerUser>));
            // builder.RegisterAssemblyTypes(this.GetType().Assembly).AsClosedTypesOf(typeof(BaseRepository<>)).InstancePerDependency();
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
                .SubscribeCommand<CreateApplicationUser>((c, e) =>
                   new CreateApplicationUserRejected(e.Message, ""))
                .SubscribeCommand<InitializeTenant>((c, e) =>
                   new InitializeTenantRejected(e.Message, "")
                );


            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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