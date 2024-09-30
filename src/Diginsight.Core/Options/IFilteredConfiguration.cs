using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

public interface IFilteredConfiguration : IConfiguration
{
    Type Class { get; }
}
