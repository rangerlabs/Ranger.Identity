using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ranger.Common;
using Ranger.Identity.Data;

namespace Ranger.Identity
{
    public interface ITenantContextProvider
    {
        (DbContextOptions<RangerIdentityDbContext> options, TenantOrganizationNameModel databaseUsername) GetDbContextOptions(string tenant);
    }
}