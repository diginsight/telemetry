namespace Diginsight.CAOptions;

public interface IVolatileConfigurationStorageProvider
{
    IVolatileConfigurationStorage this[string name] { get; }
}
