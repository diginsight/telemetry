namespace Diginsight.CAOptions;

public interface IVolatileConfigurationStorageProvider
{
    IVolatileConfigurationStorage Get(string name);
}
