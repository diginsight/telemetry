namespace Diginsight.Options;

public interface IVolatileConfigurationStorageProvider
{
    IVolatileConfigurationStorage Get(string name);
}
