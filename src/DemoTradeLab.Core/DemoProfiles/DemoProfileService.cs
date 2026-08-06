namespace DemoTradeLab.Core.DemoProfiles;

public sealed class DemoProfileService(IDemoProfileRepository repository)
{
    public Task<IReadOnlyList<DemoProfile>> ListAsync(
        CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);
}
