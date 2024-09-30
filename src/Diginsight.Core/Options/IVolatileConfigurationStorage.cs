using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

public interface IVolatileConfigurationStorage
{
    IConfiguration Configuration { get; }

    void Apply(IEnumerable<KeyValuePair<string, string?>> entries, bool overwrite = false);
}
