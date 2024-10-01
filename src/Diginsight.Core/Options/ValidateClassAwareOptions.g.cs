#nullable enable
using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public class ValidateClassAwareOptions<TOptions> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
{
    public string? Name { get; }

    public Func<Type, TOptions, ValidateOptionsResult> Validation { get; }

    public ValidateClassAwareOptions(
        string? name,
        Func<Type, TOptions, ValidateOptionsResult> validation
    )
    {
        Name = name;
        Validation = validation;
    }

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options);
    }
}

public class ValidateClassAwareOptions<TOptions, TDep1> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public Func<Type, TOptions, TDep1, ValidateOptionsResult> Validation { get; }

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

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1);
    }
}

public class ValidateClassAwareOptions<TOptions, TDep1, TDep2> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public Func<Type, TOptions, TDep1, TDep2, ValidateOptionsResult> Validation { get; }

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

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2);
    }
}

public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3> : IValidateClassAwareOptions<TOptions>
    where TOptions : class
    where TDep1 : class
    where TDep2 : class
    where TDep3 : class
{
    public string? Name { get; }

    public TDep1 Dependency1 { get; }

    public TDep2 Dependency2 { get; }

    public TDep3 Dependency3 { get; }

    public Func<Type, TOptions, TDep1, TDep2, TDep3, ValidateOptionsResult> Validation { get; }

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

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3);
    }
}

public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IValidateClassAwareOptions<TOptions>
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

    public Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, ValidateOptionsResult> Validation { get; }

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

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3, Dependency4);
    }
}

public class ValidateClassAwareOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IValidateClassAwareOptions<TOptions>
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

    public Func<Type, TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, ValidateOptionsResult> Validation { get; }

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

    public virtual ValidateOptionsResult Validate(string name, Type @class, TOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (Name is not null && name != Name)
            return ValidateOptionsResult.Skip;

        return Validation(@class, options, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5);
    }
}

