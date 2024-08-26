using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Equator = System.Func<object, object, bool>;

namespace Diginsight.Equality;

public sealed class FlexibleEqualityComparer : IEqualityComparer<object>
{
    public static readonly EqualityTypeContractAccessor ContractAccessor = new ();
    public static readonly IEqualityComparer<object> Instance = new FlexibleEqualityComparer();

    private FlexibleEqualityComparer() { }

    public new bool Equals(object? obj1, object? obj2)
    {
        return EqualsCore(obj1, obj2, null);
    }

    private static bool EqualsCore(object? obj1, object? obj2, IEquatableMemberDescriptor? outerDescriptor)
    {
        if (obj1 is null && obj2 is null)
        {
            return true;
        }
        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        UnwrapProxy(ref obj1, out IEquatableObjectDescriptor descriptor1, outerDescriptor);
        UnwrapProxy(ref obj2, out IEquatableObjectDescriptor descriptor2, outerDescriptor);

        EqualityBehavior behavior1 = descriptor1.Behavior;
        EqualityBehavior behavior2 = descriptor2.Behavior;
        if (behavior1 != behavior2)
        {
            throw new ArgumentException($"Inputs have different equality behaviors ({behavior1:G} vs {behavior2:G})");
        }

        if (behavior1 == EqualityBehavior.Forbidden)
        {
            throw new ArgumentException("Equality comparison is forbidden");
        }

        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        switch (behavior1)
        {
            case EqualityBehavior.Default:
                return obj1.Equals(obj2);

            case EqualityBehavior.Identity:
                return false;

            case EqualityBehavior.Comparer:
            {
                IComparerEquatableObjectDescriptor comparerDescriptor = (IComparerEquatableObjectDescriptor)descriptor1;
                IComparerEquatableObjectDescriptor comparerDescriptor2 = (IComparerEquatableObjectDescriptor)descriptor2;
                if (comparerDescriptor.ComparerType != comparerDescriptor2.ComparerType ||
                    !string.Equals(comparerDescriptor.ComparerMember, comparerDescriptor2.ComparerMember) ||
                    !((IStructuralEquatable)comparerDescriptor.ComparerArgs).Equals(comparerDescriptor2.ComparerArgs, EqualityComparer<object>.Default))
                {
                    throw new ArgumentException("Inputs have different comparer descriptors");
                }

                object? maybeComparer = ResolveComparer(comparerDescriptor.ComparerType, comparerDescriptor.ComparerMember, comparerDescriptor.ComparerArgs);
                if (maybeComparer is null)
                {
                    throw new ArgumentException("Comparer descriptor resolved to null");
                }

                Type[] comparedTypes = maybeComparer.GetType().GetGenericArgumentsAs(typeof(IEqualityComparer<>))
                    .Select(static x => x[0]).ToArray();
                Type? comparedType = comparedTypes.FirstOrDefault(t => IsBindable(t, obj1) && IsBindable(t, obj2));
                if (comparedType is not null)
                {
                    return (bool)typeof(IEqualityComparer<>)
                        .MakeGenericType(comparedType)
                        .GetMethod(nameof(IEqualityComparer.Equals))!
                        .Invoke(maybeComparer, [ obj1, obj2 ])!;
                }

                if (maybeComparer is IEqualityComparer comparer)
                {
                    return comparer.Equals(obj1, obj2);
                }

                throw new ArgumentException("Comparer descriptor resolved to something incompatible");
            }

            case EqualityBehavior.Structural:
            {
                Type type = obj1.GetType();
                if (type != obj2.GetType())
                {
                    throw new ArgumentException("Cannot use structural equality between objects of different type");
                }

                IEnumerable<(Equator Equator, int Order)> fieldEquatorsWithOrder = MakeEquatorsWithOrder(
                    type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    static _ => true,
                    static (f, o) => f.GetValue(o)
                );
                IEnumerable<(Equator Equator, int Order)> propertyEquatorsWithOrder = MakeEquatorsWithOrder(
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    static p => p.GetMethod is not null && p.GetIndexParameters().Length == 0,
                    static (p, o) => p.GetValue(o)
                );

                return fieldEquatorsWithOrder.Concat(propertyEquatorsWithOrder)
                    .OrderByDescending(static x => x.Order)
                    .All(x => x.Equator(obj1, obj2));
            }

            case EqualityBehavior.Forbidden:
            case EqualityBehavior.Proxy:
                throw new UnreachableException($"{nameof(EqualityBehavior)}.{behavior1:G} already handled");

            default:
                throw new UnreachableException($"Unrecognized {nameof(EqualityBehavior)}");
        }
    }

    private static IEnumerable<(Equator Equator, int Order)> MakeEquatorsWithOrder<TMember>(
        TMember[] members, Func<TMember, bool> isReadable, Func<TMember, object, object?> getValue
    )
        where TMember : MemberInfo
    {
        return members
            .Where(static x => !x.IsDefined(typeof(CompilerGeneratedAttribute)))
            .Where(isReadable)
            .Select(static x => (Member: x, Descriptor: FindMemberDescriptor(x)))
            .Where(static x => x.Descriptor.Behavior != EqualityBehavior.Forbidden)
            .Select(
                x =>
                {
                    TMember member = x.Member;
                    IEquatableMemberDescriptor descriptor = x.Descriptor;

                    return (
                        (Equator)((outerObj1, outerObj2) => EqualsCore(getValue(member, outerObj1), getValue(member, outerObj2), descriptor)),
                        descriptor.Order ?? 0
                    );
                }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBindable(Type t, object? o)
    {
        return o is not null
            ? t.IsInstanceOfType(o)
            : !t.IsValueType || Nullable.GetUnderlyingType(t) is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCallable(ParameterInfo[] parameters, object?[] args)
    {
        return parameters
#if NET
            .Zip(args)
#else
            .Zip(args, static (p, o) => (First: p, Second: o))
#endif
            .All(static x => IsBindable(x.First.ParameterType, x.Second));
    }

    private static object? ResolveComparer(Type type, string? memberName, object?[] args)
    {
        if (memberName is null)
        {
            ConstructorInfo? ctor = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c => IsCallable(c.GetParameters(), args));
            return ctor is not null
                ? ctor.Invoke(args)
                : throw new ArgumentException($"Cannot resolve comparer descriptor {type}");
        }

        if (args.Length <= 0 && type.GetField(memberName, BindingFlags.Public | BindingFlags.Static) is { } field)
        {
            return field.GetValue(null);
        }

        PropertyInfo? property = type
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(p => p.Name == memberName && p.CanRead && IsCallable(p.GetIndexParameters(), args));
        if (property is not null)
        {
            return property.GetValue(null, args);
        }

        MethodInfo? method = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(p => p.Name == memberName && !p.IsGenericMethod && IsCallable(p.GetParameters(), args));
        if (method is not null)
        {
            return method.Invoke(null, args);
        }

        throw new ArgumentException($"Cannot resolve comparer descriptor {type}.{memberName}");
    }

    private static object? ResolveProxy(object self, Type type, string? memberName, object?[] args)
    {
        object?[] finalArgs;
        if (memberName is null)
        {
            finalArgs = [ self, ..args ];

            ConstructorInfo? ctor = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c => IsCallable(c.GetParameters(), finalArgs));
            return ctor is not null
                ? ctor.Invoke(finalArgs)
                : throw new ArgumentException($"Cannot resolve comparer descriptor {type}");
        }

        Type finalType;
        BindingFlags bindingFlags;
        object? target;

        if (type == typeof(void))
        {
            finalType = self.GetType();
            finalArgs = args;
            bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            target = self;

            if (finalArgs.Length <= 0 && finalType.GetField(memberName, bindingFlags) is { } field)
            {
                return field.GetValue(target);
            }
        }
        else
        {
            finalType = type;
            finalArgs = [ self, ..args ];
            bindingFlags = BindingFlags.Public | BindingFlags.Static;
            target = null;
        }

        PropertyInfo? property = finalType.GetProperties(bindingFlags)
            .FirstOrDefault(p => p.Name == memberName && p.CanRead && IsCallable(p.GetIndexParameters(), finalArgs));
        if (property is not null)
        {
            return property.GetValue(target, finalArgs);
        }

        MethodInfo? method = finalType.GetMethods(bindingFlags)
            .FirstOrDefault(p => p.Name == memberName && !p.IsGenericMethod && IsCallable(p.GetParameters(), finalArgs));
        if (method is not null)
        {
            return method.Invoke(target, finalArgs);
        }

        throw new ArgumentException($"Cannot resolve proxy descriptor {(type == typeof(void) ? "" : $"{type}.")}{memberName}");
    }

    private static void UnwrapProxy(ref object obj, out IEquatableObjectDescriptor descriptor, IEquatableMemberDescriptor? outerDescriptor)
    {
        while (true)
        {
            descriptor = outerDescriptor?.TryToObjectDescriptor() ?? FindTypeDescriptor(obj.GetType());
            if (descriptor.Behavior != EqualityBehavior.Proxy)
            {
                return;
            }

            IProxyEquatableDescriptor proxyDescriptor = (IProxyEquatableDescriptor)descriptor;
            obj = ResolveProxy(obj, proxyDescriptor.ProxyType, proxyDescriptor.ProxyMember, proxyDescriptor.ProxyArgs)
                ?? throw new ArgumentException("Proxy descriptor resolved to null");
        }
    }

    private static IEquatableObjectDescriptor FindTypeDescriptor(Type type)
    {
        foreach (Type t in type.GetClosure())
        {
            if (ContractAccessor.TryGet(t) is { Behavior: not null } typeContract)
            {
                return typeContract.ToObjectDescriptor();
            }

            EquatableObjectAttribute[] attributes = t.GetCustomAttributes<EquatableObjectAttribute>().Take(2).ToArray();
            switch (attributes)
            {
                case [ ]:
                    break;

                case [ var attribute ]:
                    return attribute;

                case [ _, _ ]:
                    throw new ArgumentException($"Multiple {nameof(EquatableObjectAttribute)}s applied to type {t}");
            }
        }

        return EqualityTypeContract.FallbackDescriptorFor(type);
    }

    private static IEquatableMemberDescriptor FindMemberDescriptor(MemberInfo member)
    {
        foreach (Type t in member.ReflectedType!.GetClosure())
        {
            if (ContractAccessor.TryGet(t) is { } typeContract && typeContract.TryGet(member) is { } memberContract)
            {
                return memberContract.ToMemberDescriptor();
            }

            EquatableMemberAttribute[] attributes = t.GetCustomAttributes<EquatableMemberAttribute>().Take(2).ToArray();
            switch (attributes)
            {
                case [ ]:
                    break;

                case [ var attribute ]:
                    return attribute;

                case [ _, _ ]:
                    throw new ArgumentException($"Multiple {nameof(EquatableMemberAttribute)}s applied to member {t}.{member.Name}");
            }

            if (member.DeclaringType == t)
                break;
        }

        return EqualityMemberContract.EmptyDescriptor;
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
