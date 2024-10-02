#nullable enable
namespace Diginsight.Options;

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
public class ConfigureClassAwareOptions<TOptions> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        Action<Type, TOptions> action
    )
    {
        Name = name;
        Action = action;
    }

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options);
    }
}

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/> with 1 dependency.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
public class ConfigureClassAwareOptions<TOptions, TDep1> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for configuration.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions, TDep1> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions, TDep1}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="dependency1">The 1st dependency required for configuration.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        Action<Type, TOptions, TDep1> action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Action = action;
    }

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options, Dependency1);
    }
}

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/> with 2 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
public class ConfigureClassAwareOptions<TOptions, TDep1, TDep2> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for configuration.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for configuration.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions, TDep1, TDep2> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions, TDep1, TDep2}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="dependency1">The 1st dependency required for configuration.</param>
    /// <param name="dependency2">The 2nd dependency required for configuration.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        Action<Type, TOptions, TDep1, TDep2> action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Action = action;
    }

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options, Dependency1, Dependency2);
    }
}

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/> with 3 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
public class ConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for configuration.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for configuration.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for configuration.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions, TDep1, TDep2, TDep3> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions, TDep1, TDep2, TDep3}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="dependency1">The 1st dependency required for configuration.</param>
    /// <param name="dependency2">The 2nd dependency required for configuration.</param>
    /// <param name="dependency3">The 3rd dependency required for configuration.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        Action<Type, TOptions, TDep1, TDep2, TDep3> action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Action = action;
    }

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options, Dependency1, Dependency2, Dependency3);
    }
}

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/> with 4 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
/// <typeparam name="TDep4">The type of the 4th dependency required for configuration.</typeparam>
public class ConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for configuration.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for configuration.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for configuration.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the 4th dependency required for configuration.
    /// </summary>
    public TDep4 Dependency4 { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions, TDep1, TDep2, TDep3, TDep4}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="dependency1">The 1st dependency required for configuration.</param>
    /// <param name="dependency2">The 2nd dependency required for configuration.</param>
    /// <param name="dependency3">The 3rd dependency required for configuration.</param>
    /// <param name="dependency4">The 4th dependency required for configuration.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4> action
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Dependency4 = dependency4;
        Action = action;
    }

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options, Dependency1, Dependency2, Dependency3, Dependency4);
    }
}

/// <summary>
///     Provides configuration for options of type <typeparamref name="TOptions"/> with 5 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
/// <typeparam name="TDep4">The type of the 4th dependency required for configuration.</typeparam>
/// <typeparam name="TDep5">The type of the 5th dependency required for configuration.</typeparam>
public class ConfigureClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IConfigureClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
    where TDep5 : class
{
    /// <summary>
    ///     Gets the name of the options instance being configured.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for configuration.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for configuration.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for configuration.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the 4th dependency required for configuration.
    /// </summary>
    public TDep4 Dependency4 { get; }

    /// <summary>
    ///     Gets the 5th dependency required for configuration.
    /// </summary>
    public TDep5 Dependency5 { get; }

    /// <summary>
    ///     Gets the action to be performed for configuration.
    /// </summary>
    public Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> Action { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigureClassAwareOptions{TOptions, TDep1, TDep2, TDep3, TDep4, TDep5}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="dependency1">The 1st dependency required for configuration.</param>
    /// <param name="dependency2">The 2nd dependency required for configuration.</param>
    /// <param name="dependency3">The 3rd dependency required for configuration.</param>
    /// <param name="dependency4">The 4th dependency required for configuration.</param>
    /// <param name="dependency5">The 5th dependency required for configuration.</param>
    /// <param name="action">The action to be performed for configuration.</param>
    public ConfigureClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        TDep5 dependency5,
        Action<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> action
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

    /// <inheritdoc />
    public virtual void Configure(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return;

        Action(@class, options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5);
    }
}

