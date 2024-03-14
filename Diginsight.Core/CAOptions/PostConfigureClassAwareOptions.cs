namespace Diginsight.CAOptions;

public class PostConfigureClassAwareOptions<TOptions> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public string? Name { get; }

    public Action<Type, TOptions>? Action { get; }

    public PostConfigureClassAwareOptions(string? name, Action<Type, TOptions>? action)
    {
        Name = name;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options);
    }
}
