namespace Diginsight.CAOptions;

public abstract class VolatileConfigurationLoader
{
    private readonly IVolatileConfigurationStorage storage;

    protected VolatileConfigurationLoader(IVolatileConfigurationStorage storage)
    {
        this.storage = storage;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        storage.Apply(await LoadImplAsync(cancellationToken));
    }

    protected abstract Task<IEnumerable<KeyValuePair<string, string?>>> LoadImplAsync(CancellationToken cancellationToken = default);
}
