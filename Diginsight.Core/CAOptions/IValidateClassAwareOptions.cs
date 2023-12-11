using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IValidateClassAwareOptions<in TOptions>
    where TOptions : class
{
    ValidateOptionsResult Validate(string name, Type @class, TOptions options);
}
