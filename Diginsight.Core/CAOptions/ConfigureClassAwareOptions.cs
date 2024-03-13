namespace Diginsight.CAOptions;

public class ConfigureClassAwareOptions<TOptions> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public string? Name { get; }

    public Action<Type, TOptions>? Action { get; }

    public ConfigureClassAwareOptions(string? name, Action<Type, TOptions>? action)
    {
        Name = name;
        Action = action;
    }

    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options);
    }
}
