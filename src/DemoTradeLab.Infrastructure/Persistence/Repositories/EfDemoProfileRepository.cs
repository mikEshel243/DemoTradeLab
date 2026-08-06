using DemoTradeLab.Core.DemoProfiles;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Repositories;

internal sealed class EfDemoProfileRepository(DemoTradeLabDbContext context)
    : IDemoProfileRepository
{
    public async Task<IReadOnlyList<DemoProfile>> ListAsync(
        CancellationToken cancellationToken) =>
        await context.DemoProfiles
            .AsNoTracking()
            .Include(profile => profile.Accounts)
            .OrderBy(profile => profile.Key)
            .ToListAsync(cancellationToken);
}
