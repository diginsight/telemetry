using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Diginsight.Stringify;

internal sealed class MemberInfoStringifier : IMemberInfoStringifier
{
    public const string CollectionLengthMetaProperty = "collectionLength";

    private static readonly IReadOnlyDictionary<Type, string> KNOWN_TYPE_NAMES = new Dictionary<Type, string>()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(nint)] = "nint",
        [typeof(nuint)] = "nuint",
        [typeof(object)] = "object",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(string)] = "string",
        [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong",
        [typeof(ushort)] = "ushort",
        [typeof(void)] = "void",
    };

    private readonly IStringifyOverallConfiguration overallConfiguration;

    public MemberInfoStringifier(
        IOptions<StringifyOverallConfiguration> overallConfigurationOptions
    )
    {
        overallConfiguration = overallConfigurationOptions.Value;
    }

    public IStringifiable? TryStringify(object obj)
    {
        return obj switch
        {
            Type type => new StringifiableType(type, this),
            MemberInfo member => new StringifiableMember(member, this),
            ParameterInfo parameter => new StringifiableParameter(parameter, this),
            Assembly assembly => new StringifiableAssembly(assembly),
            _ => null,
        };
    }

    public void Append(Type type, StringifyContext stringifyContext)
    {
        void AppendNamespace(string? ns)
        {
            if (ns == null)
                return;

            IStringifyNamespaceConfiguration namespaceConfiguration = stringifyContext.VariableConfiguration;

            bool isImplicit = namespaceConfiguration.ImplicitNamespaces?.IsMatch(ns) ?? false;
            bool isExplicit = namespaceConfiguration.ExplicitNamespaces?.IsMatch(ns) ?? false;

            if ((isImplicit && isExplicit && namespaceConfiguration.IsNamespaceExplicitIfAmbiguous) ||
                (!isImplicit && !isExplicit && namespaceConfiguration.IsNamespaceExplicitIfUnspecified) ||
                isExplicit)
            {
                stringifyContext
                    .AppendDirect(ns)
                    .AppendDirect('.');
            }
        }

        _ = stringifyContext.MetaProperties.TryGetValue(CollectionLengthMetaProperty, out object? rawCollectionLength);

        using IDisposable _0 = stringifyContext.WithMetaProperties(static x => { x.Remove(CollectionLengthMetaProperty); });

        if (type.IsArray)
        {
            Append(type.GetElementType()!, stringifyContext);
            stringifyContext.AppendDelimited(
                '[',
                ']',
                sc =>
                {
                    if (rawCollectionLength is int[] arrayLengths)
                    {
                        using IEnumerator<int> enumerator = arrayLengths.AsEnumerable().GetEnumerator();
                        sc.AppendEnumerator(
                            enumerator,
                            static (sc1, e) => { sc1.AppendDirect(e.Current.ToStringInvariant()); },
                            AllottedCounter.Unlimited,
                            ","
                        );
                    }
                    else
                    {
                        sc.AppendDirect(new string(',', type.GetArrayRank() - 1));
                    }
                }
            );
        }
        else if (type.IsPointer)
        {
            Append(type.GetElementType()!, stringifyContext);
            stringifyContext.AppendDirect('*');
        }
        else if (type.IsByRef)
        {
            Append(type.GetElementType()!, stringifyContext);
            stringifyContext.AppendDirect('&');
        }
        else if (type.IsNested)
        {
            Append(type.DeclaringType!, stringifyContext);
            stringifyContext
                .AppendDirect('+')
                .AppendDirect(type.Name);
        }
        else if (type.IsGenericParameter)
        {
            stringifyContext.AppendDirect(type.Name);
        }
        else if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            Append(nullableType, stringifyContext);
            stringifyContext.AppendDirect('?');
        }
        else if (type.IsAnonymous())
        {
            stringifyContext.AppendDirect('¤');
        }
        else if (IsValueTuple(type, out Type[]? itemTypes))
        {
            stringifyContext.AppendDelimited(
                StringifyTokens.TupleBegin,
                StringifyTokens.TupleEnd,
                sc =>
                {
                    using IEnumerator<Type> enumerator = itemTypes.AsEnumerable().GetEnumerator();
                    sc.AppendEnumerator(
                        enumerator,
                        (ac1, e) => { Append(e.Current, ac1); },
                        AllottedCounter.Unlimited,
                        ","
                    );
                }
            );
        }
        else if (type.IsGenericType)
        {
            AppendNamespace(type.Namespace);
            stringifyContext
                .AppendDirect(type.Name[..type.Name.IndexOf('`')])
                .AppendDelimited(
                    '<',
                    '>',
                    sc =>
                    {
                        if (type.IsGenericTypeDefinition)
                        {
                            sc.AppendDirect(new string(',', type.GetGenericArguments().Length - 1));
                        }
                        else
                        {
                            using IEnumerator<Type> enumerator = type.GetGenericArguments().AsEnumerable().GetEnumerator();
                            sc.AppendEnumerator(
                                enumerator,
                                (ac1, e) => { Append(e.Current, ac1); },
                                AllottedCounter.Unlimited,
                                ","
                            );
                        }
                    }
                );
        }
        else
        {
            if (overallConfiguration.ShortenKnownTypes && KNOWN_TYPE_NAMES.TryGetValue(type, out string? name))
            {
                stringifyContext.AppendDirect(name);
            }
            else
            {
                AppendNamespace(type.Namespace);
                stringifyContext.AppendDirect(type.Name);
            }
        }

        if (rawCollectionLength is int collectionLength)
        {
            stringifyContext.AppendDirect('(').AppendDirect(collectionLength.ToStringInvariant()).AppendDirect(')');
        }
    }

    private static bool IsValueTuple(Type type, [NotNullWhen(true)] out Type[]? itemTypes)
    {
        if (type.FullName?.StartsWith(typeof(ValueTuple).FullName!) != true)
        {
            itemTypes = null;
            return false;
        }

        itemTypes = type == typeof(ValueTuple) ? Type.EmptyTypes : type.GetGenericArguments();
        return true;
    }

    public void Append(ParameterInfo[] parameters, StringifyContext stringifyContext)
    {
        stringifyContext
            .AppendDelimited(
                '(',
                ')',
                sc =>
                {
                    using IEnumerator<ParameterInfo> enumerator = ((IReadOnlyList<ParameterInfo>)parameters).GetEnumerator();
                    sc.AppendEnumerator(
                        enumerator,
                        (ac1, e) => { Append(e.Current, ac1); },
                        AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxMethodParameterCount())
                    );
                }
            );
    }

    private void Append(ParameterInfo parameter, StringifyContext stringifyContext)
    {
        if (parameter.IsIn)
        {
            stringifyContext.AppendDirect('↓');
        }
        if (parameter.IsOut)
        {
            stringifyContext.AppendDirect('↑');
        }
        Append(parameter.ParameterType, stringifyContext);
    }

    private sealed class StringifiableType : IStringifiable
    {
        private readonly Type type;
        private readonly MemberInfoStringifier owner;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object IStringifiable.Subject => type;

        public StringifiableType(Type type, MemberInfoStringifier owner)
        {
            this.type = type;
            this.owner = owner;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            owner.Append(type, stringifyContext);
        }
    }

    private sealed class StringifiableMember : IStringifiable
    {
        private readonly MemberInfo member;
        private readonly MemberInfoStringifier owner;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object IStringifiable.Subject => member;

        public StringifiableMember(MemberInfo member, MemberInfoStringifier owner)
        {
            this.member = member;
            this.owner = owner;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            if (member.DeclaringType is { } declaringType)
            {
                owner.Append(declaringType, stringifyContext);
                stringifyContext.AppendDirect('#');
            }

            stringifyContext.AppendDirect(member.Name);

            ParameterInfo[]? parameters;
            if (member is MethodBase method)
            {
                parameters = method.GetParameters();
            }
            else if (member is PropertyInfo property)
            {
                ParameterInfo[] propertyParameters = property.GetIndexParameters();
                parameters = propertyParameters.Length > 0 ? propertyParameters : null;
            }
            else
            {
                parameters = null;
            }

            if (parameters != null)
            {
                owner.Append(parameters, stringifyContext);
            }
        }
    }

    private sealed class StringifiableParameter : IStringifiable
    {
        private readonly ParameterInfo parameter;
        private readonly MemberInfoStringifier owner;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableParameter(ParameterInfo parameter, MemberInfoStringifier owner)
        {
            this.parameter = parameter;
            this.owner = owner;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            owner.Append(parameter, stringifyContext);
        }
    }

    private sealed class StringifiableAssembly : IStringifiable
    {
        private readonly Assembly assembly;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableAssembly(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect(assembly.GetName().FullName);
        }
    }
}
