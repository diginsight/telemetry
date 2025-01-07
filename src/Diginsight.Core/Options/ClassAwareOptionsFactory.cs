using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Default implementation of the <see cref="IClassAwareOptionsFactory{TOptions}" /> interface.
/// </summary>
/// <typeparam name="TOptions">The type of options to cache.</typeparam>
public sealed class ClassAwareOptionsFactory<TOptions> : IClassAwareOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IConfigureOptions<TOptions>[] configurators;
    private readonly IConfigureClassAwareOptions<TOptions>[] classAwareConfigurators;
    private readonly IPostConfigureOptions<TOptions>[] postConfigurators;
    private readonly IPostConfigureClassAwareOptions<TOptions>[] classAwarePostConfigurators;
    private readonly IValidateOptions<TOptions>[] validators;
    private readonly IValidateClassAwareOptions<TOptions>[] classAwareValidators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsFactory{TOptions}" /> class.
    /// </summary>
    /// <param name="configurators">The configurators to configure options instances.</param>
    /// <param name="classAwareConfigurators">The class-aware configurators to configure options instances.</param>
    /// <param name="postConfigurators">The post-configurators to post-configure options instances.</param>
    /// <param name="classAwarePostConfigurators">The class-aware post-configurators to post-configure options instances.</param>
    /// <param name="validators">The validators to validate options instances.</param>
    /// <param name="classAwareValidators">The class-aware validators to validate options instances.</param>
    public ClassAwareOptionsFactory(
        IEnumerable<IConfigureOptions<TOptions>> configurators,
        IEnumerable<IConfigureClassAwareOptions<TOptions>> classAwareConfigurators,
        IEnumerable<IPostConfigureOptions<TOptions>> postConfigurators,
        IEnumerable<IPostConfigureClassAwareOptions<TOptions>> classAwarePostConfigurators,
        IEnumerable<IValidateOptions<TOptions>> validators,
        IEnumerable<IValidateClassAwareOptions<TOptions>> classAwareValidators
    )
    {
        this.configurators = configurators.ToArray();
        this.classAwareConfigurators = classAwareConfigurators.ToArray();
        this.postConfigurators = postConfigurators.ToArray();
        this.classAwarePostConfigurators = classAwarePostConfigurators.ToArray();
        this.validators = validators.ToArray();
        this.classAwareValidators = classAwareValidators.ToArray();
    }

    /// <inheritdoc />
    public TOptions Create(string name, Type @class)
    {
        TOptions options = Activator.CreateInstance<TOptions>();

        foreach (IConfigureOptions<TOptions> configurator in configurators)
        {
            if (configurator is IConfigureNamedOptions<TOptions> namedConfigurator)
            {
                namedConfigurator.Configure(name, options);
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

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsFactory<TOptions>.Create(string name) => Create(name, ClassAwareOptions.NoClass);
#endif
}
