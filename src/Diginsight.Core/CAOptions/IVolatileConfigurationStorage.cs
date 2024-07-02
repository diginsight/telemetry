using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public interface IVolatileConfigurationStorage
{
    IConfiguration Configuration { get; }

    void Apply(IEnumerable<KeyValuePair<string, string?>> entries, bool overwrite = false);
}
