using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Ranger.Identity {
    public class ConfigurationDbContextInitializer : IConfigurationDbContextInitializer {
        private readonly ConfigurationDbContext context;
        public ConfigurationDbContextInitializer (ConfigurationDbContext context) {
            this.context = context;
        }
        public bool EnsureCreated () {
            return context.Database.EnsureCreated ();
        }

        public void Migrate () {
            context.Database.Migrate ();
        }

        public async Task SeedAsync () {
            if (!context.Clients.Any ()) {
                foreach (var client in Config.GetClients ()) {
                    context.Clients.Add (client.ToEntity ());
                }
            }

            if (!context.IdentityResources.Any ()) {
                foreach (var resource in Config.GetIdentityResources ()) {
                    context.IdentityResources.Add (resource.ToEntity ());
                }
            }

            if (!context.ApiResources.Any ()) {
                foreach (var resource in Config.GetApiResources ()) {
                    context.ApiResources.Add (resource.ToEntity ());
                }
            }
            await context.SaveChangesAsync ();
        }
    }

    public interface IConfigurationDbContextInitializer {
        bool EnsureCreated ();
        void Migrate ();
        Task SeedAsync ();
    }
}