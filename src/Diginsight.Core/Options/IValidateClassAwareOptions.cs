using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public interface IValidateClassAwareOptions<in TOptions>
    where TOptions : class
{
    ValidateOptionsResult Validate(string name, Type @class, TOptions options);
}
