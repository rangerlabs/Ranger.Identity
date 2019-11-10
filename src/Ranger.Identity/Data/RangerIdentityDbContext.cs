using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ranger.Identity.Data
{
    public class RangerIdentityDbContext : IdentityDbContext<RangerUser,
                                                     IdentityRole,
                                                     string>, IDataProtectionKeyContext
    {
        public delegate RangerIdentityDbContext Factory(DbContextOptions<RangerIdentityDbContext> options);

        public RangerIdentityDbContext(DbContextOptions<RangerIdentityDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            var user = builder.Entity<RangerUser>(builder =>
            {
                builder.Metadata.RemoveIndex(new[] { builder.Property(u => u.NormalizedUserName).Metadata });
                builder.HasIndex(u => new { u.DatabaseUsername, u.NormalizedUserName }).HasName("UserNameIndex").IsUnique();
            });
        }
    }
}
