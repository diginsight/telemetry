using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsFactory<TOptions> : IClassAwareOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IOptionsFactory<TOptions> decoratee;
    private readonly IConfigureOptions<TOptions>[] configurators;
    private readonly IPostConfigureOptions<TOptions>[] postConfigurators;
    private readonly IValidateOptions<TOptions>[] validators;
    private readonly IConfigureClassAwareOptions<TOptions>[] classAwareConfigurators;
    private readonly IPostConfigureClassAwareOptions<TOptions>[] classAwarePostConfigurators;
    private readonly IValidateClassAwareOptions<TOptions>[] classAwareValidators;

    public ClassAwareOptionsFactory(
        IOptionsFactory<TOptions> decoratee,
        IEnumerable<IConfigureOptions<TOptions>> configurators,
        IEnumerable<IPostConfigureOptions<TOptions>> postConfigurators,
        IEnumerable<IValidateOptions<TOptions>> validators,
        IEnumerable<IConfigureClassAwareOptions<TOptions>> classAwareConfigurators,
        IEnumerable<IPostConfigureClassAwareOptions<TOptions>> classAwarePostConfigurators,
        IEnumerable<IValidateClassAwareOptions<TOptions>> classAwareValidators
    )
    {
        this.decoratee = decoratee;
        this.configurators = configurators.ToArray();
        this.postConfigurators = postConfigurators.ToArray();
        this.validators = validators.ToArray();
        this.classAwareConfigurators = classAwareConfigurators.ToArray();
        this.classAwarePostConfigurators = classAwarePostConfigurators.ToArray();
        this.classAwareValidators = classAwareValidators.ToArray();
    }

    public TOptions Create(string name, Type? @class)
    {
        if (@class is null)
        {
            return decoratee.Create(name);
        }

        TOptions options = Activator.CreateInstance<TOptions>();

        foreach (IConfigureOptions<TOptions> configurator in configurators)
        {
            if (configurator is IConfigureNamedOptions<TOptions> namedConfigurer)
            {
                namedConfigurer.Configure(name, options);
            }
            else
            {
                configurator.Configure(options);
            }
        }

        foreach (IConfigureClassAwareOptions<TOptions> configurator in classAwareConfigurators)
        {
            configurator.Configure(name, @class, options);
        }

        foreach (IPostConfigureOptions<TOptions> postConfigurator in postConfigurators)
        {
            postConfigurator.PostConfigure(name, options);
        }

        foreach (IPostConfigureClassAwareOptions<TOptions> postConfigurator in classAwarePostConfigurators)
        {
            postConfigurator.PostConfigure(name, @class, options);
        }

        ICollection<string> failureMessages = new List<string>();

        foreach (IValidateOptions<TOptions> validator in validators)
        {
            ValidateOptionsResult validateOptionsResult = validator.Validate(name, options);
            if (validateOptionsResult.Failed)
            {
                failureMessages.AddRange(validateOptionsResult.Failures);
            }
        }

        foreach (IValidateClassAwareOptions<TOptions> validator in classAwareValidators)
        {
            ValidateOptionsResult validateOptionsResult = validator.Validate(name, @class, options);
            if (validateOptionsResult.Failed)
            {
                failureMessages.AddRange(validateOptionsResult.Failures);
            }
        }

        if (failureMessages.Count > 0)
        {
            throw new OptionsValidationException(name, typeof(TOptions), failureMessages);
        }

        return options;
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsFactory<TOptions>.Create(string name) => Create(name, null);
#endif
}
