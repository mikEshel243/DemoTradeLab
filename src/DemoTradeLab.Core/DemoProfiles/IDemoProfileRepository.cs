namespace DemoTradeLab.Core.DemoProfiles;

public interface IDemoProfileRepository
{
    Task<IReadOnlyList<DemoProfile>> ListAsync(CancellationToken cancellationToken);
}
