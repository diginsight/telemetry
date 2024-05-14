#nullable enable
namespace Diginsight.CAOptions;

public class PostConfigureClassAwareOptions<TOptions> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public string? Name { get; }

    public Action<Type, TOptions>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        Action<Type, TOptions>? action
    )
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

public class PostConfigureClassAwareOptions<TOptions, TDep1> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public Action<Type, TOptions, TDep1>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        Action<Type, TOptions, TDep1>? action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options, Dependency1);
    }
}

public class PostConfigureClassAwareOptions<TOptions, TDep1, TDep2> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public Action<Type, TOptions, TDep1, TDep2>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        Action<Type, TOptions, TDep1, TDep2>? action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options, Dependency1, Dependency2);
    }
}

public class PostConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public TDep3 Dependency3 { get; }

    public Action<Type, TOptions, TDep1, TDep2, TDep3>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        Action<Type, TOptions, TDep1, TDep2, TDep3>? action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options, Dependency1, Dependency2, Dependency3);
    }
}

public class PostConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public TDep3 Dependency3 { get; }

    public TDep4 Dependency4 { get; }

    public Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4>? action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Dependency4 = dependency4;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options, Dependency1, Dependency2, Dependency3, Dependency4);
    }
}

public class PostConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
    where TDep5 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public TDep3 Dependency3 { get; }

    public TDep4 Dependency4 { get; }

    public TDep5 Dependency5 { get; }

    public Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5>? Action { get; }

    public PostConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        TDep5 dependency5,
        Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5>? action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Dependency4 = dependency4;
        Dependency5 = dependency5;
        Action = action;
    }

    public virtual void PostConfigure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action?.Invoke(@class, options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5);
    }
}

