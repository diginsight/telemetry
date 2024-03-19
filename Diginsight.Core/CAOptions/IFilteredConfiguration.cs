using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public interface IFilteredConfiguration : IConfiguration
{
    Type Class { get; }
}
