using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

internal interface IFilteredConfiguration : IConfiguration
{
    Type Class { get; }
}
