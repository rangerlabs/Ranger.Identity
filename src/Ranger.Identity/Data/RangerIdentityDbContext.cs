using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ranger.Common;
using Ranger.RabbitMQ;

namespace Ranger.Identity.Data
{
    public class RangerIdentityDbContext : IdentityDbContext<RangerUser, IdentityRole, string>, IDataProtectionKeyContext, IOutboxStore
    {
        private readonly IDataProtectionProvider dataProtectionProvider;

        public RangerIdentityDbContext(DbContextOptions<RangerIdentityDbContext> options, IDataProtectionProvider dataProtectionProvider = null)
            : base(options)
        {
            this.dataProtectionProvider = dataProtectionProvider;
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<OutboxMessage> Outbox { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Remove 'AspNet' prefix and convert table name from PascalCase to snake_case. E.g. AspNetRoleClaims -> role_claims
                entity.SetTableName(entity.GetTableName().Replace("AspNet", "").ToSnakeCase());

                // Convert column names from PascalCase to snake_case.
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.Name.ToSnakeCase());
                }

                // Convert primary key names from PascalCase to snake_case. E.g. PK_users -> pk_users
                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().ToSnakeCase());
                }

                // Convert foreign key names from PascalCase to snake_case.
                foreach (var key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName().ToSnakeCase());
                }

                // Convert index names from PascalCase to snake_case.
                foreach (var index in entity.GetIndexes())
                {
                    index.SetName(index.GetName().ToSnakeCase());
                }
            }

            var user = modelBuilder.Entity<RangerUser>(builder =>
            {
                builder.Metadata.RemoveIndex(new[] { builder.Property(u => u.NormalizedUserName).Metadata });
                builder.Metadata.RemoveIndex(new[] { builder.Property(u => u.NormalizedEmail).Metadata });
                builder.HasIndex(u => new { u.TenantId, u.NormalizedUserName }).HasName("UserNameIndex").IsUnique();
                builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).HasName("EmailIndex").IsUnique();
            });
        }
    }
}
