#nullable enable
using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
public class ValidateClassAwareOptions<TOptions> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        Func<Type, TOptions, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options);
    }
}

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/> with 1 dependency.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
public class ValidateClassAwareOptions<TOptions, TDep1> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for validation.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, TDep1, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions, TDep1}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="dependency1">The 1st dependency required for validation.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        TDep1 dependency1,
        Func<Type, TOptions, TDep1, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1);
    }
}

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/> with 2 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
public class ValidateClassAwareOptions<TOptions, TDep1, TDep2> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for validation.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for validation.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, TDep1, TDep2, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions, TDep1, TDep2}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="dependency1">The 1st dependency required for validation.</param>
    /// <param name="dependency2">The 2nd dependency required for validation.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        Func<Type, TOptions, TDep1, TDep2, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2);
    }
}

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/> with 3 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for validation.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for validation.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for validation.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, TDep1, TDep2, TDep3, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions, TDep1, TDep2, TDep3}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="dependency1">The 1st dependency required for validation.</param>
    /// <param name="dependency2">The 2nd dependency required for validation.</param>
    /// <param name="dependency3">The 3rd dependency required for validation.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        Func<Type, TOptions, TDep1, TDep2, TDep3, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3);
    }
}

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/> with 4 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
/// <typeparam name="TDep4">The type of the 4th dependency required for configuration.</typeparam>
public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for validation.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for validation.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for validation.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the 4th dependency required for validation.
    /// </summary>
    public TDep4 Dependency4 { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions, TDep1, TDep2, TDep3, TDep4}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="dependency1">The 1st dependency required for validation.</param>
    /// <param name="dependency2">The 2nd dependency required for validation.</param>
    /// <param name="dependency3">The 3rd dependency required for validation.</param>
    /// <param name="dependency4">The 4th dependency required for validation.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Dependency4 = dependency4;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3, Dependency4);
    }
}

/// <summary>
///     Provides validation for options of type <typeparamref name="TOptions"/> with 5 dependencies.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <typeparam name="TDep1">The type of the 1st dependency required for configuration.</typeparam>
/// <typeparam name="TDep2">The type of the 2nd dependency required for configuration.</typeparam>
/// <typeparam name="TDep3">The type of the 3rd dependency required for configuration.</typeparam>
/// <typeparam name="TDep4">The type of the 4th dependency required for configuration.</typeparam>
/// <typeparam name="TDep5">The type of the 5th dependency required for configuration.</typeparam>
public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
    where TDep4 : class
    where TDep5 : class
{
    /// <summary>
    ///     Gets the name of the options instance being validated.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Gets the 1st dependency required for validation.
    /// </summary>
    public TDep1 Dependency1 { get; }

    /// <summary>
    ///     Gets the 2nd dependency required for validation.
    /// </summary>
    public TDep2 Dependency2 { get; }

    /// <summary>
    ///     Gets the 3rd dependency required for validation.
    /// </summary>
    public TDep3 Dependency3 { get; }

    /// <summary>
    ///     Gets the 4th dependency required for validation.
    /// </summary>
    public TDep4 Dependency4 { get; }

    /// <summary>
    ///     Gets the 5th dependency required for validation.
    /// </summary>
    public TDep5 Dependency5 { get; }

    /// <summary>
    ///     Gets the function to be performed for validation.
    /// </summary>
    public Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, ValidateOptionsResult> Validation { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidateClassAwareOptions{TOptions, TDep1, TDep2, TDep3, TDep4, TDep5}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="dependency1">The 1st dependency required for validation.</param>
    /// <param name="dependency2">The 2nd dependency required for validation.</param>
    /// <param name="dependency3">The 3rd dependency required for validation.</param>
    /// <param name="dependency4">The 4th dependency required for validation.</param>
    /// <param name="dependency5">The 5th dependency required for validation.</param>
    /// <param name="validation">The function to be performed for validation.</param>
    public ValidateClassAwareOptions(
        string? name,
        TDep1 dependency1,
        TDep2 dependency2,
        TDep3 dependency3,
        TDep4 dependency4,
        TDep5 dependency5,
        Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Dependency1 = dependency1;
        Dependency2 = dependency2;
        Dependency3 = dependency3;
        Dependency4 = dependency4;
        Dependency5 = dependency5;
        Validation = validation;
    }

    /// <inheritdoc />
    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5);
    }
}

