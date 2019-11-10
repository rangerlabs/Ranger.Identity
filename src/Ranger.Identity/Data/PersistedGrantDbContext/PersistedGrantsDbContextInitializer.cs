using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ranger.Identity {
    public class PersistedGrantDbContextInitializer : IPersistedGrantDbContextInitializer {
        private readonly PersistedGrantDbContext context;
        public PersistedGrantDbContextInitializer (PersistedGrantDbContext context) {
            this.context = context;
        }
        public bool EnsureCreated () {
            return context.Database.EnsureCreated ();
        }

        public void Migrate () {
            context.Database.Migrate ();
        }
    }
    public interface IPersistedGrantDbContextInitializer {
        bool EnsureCreated ();
        void Migrate ();
    }
}