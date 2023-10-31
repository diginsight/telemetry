using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Diginsight.Strings;

internal sealed class MemberInfoLogStringProvider : IMemberInfoLogStringProvider
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

    private readonly ILogStringOverallConfiguration overallConfiguration;

    public MemberInfoLogStringProvider(
        IOptions<LogStringOverallConfiguration> overallConfigurationOptions
    )
    {
        overallConfiguration = overallConfigurationOptions.Value;
    }

    public ILogStringable? TryAsLogStringable(object obj)
    {
        return obj switch
        {
            Type type => new LogStringableType(type, this),
            MemberInfo member => new LogStringableMember(member, this),
            ParameterInfo parameter => new LogStringableParameter(parameter, this),
            Assembly assembly => new LogStringableAssembly(assembly),
            _ => null,
        };
    }

    public void Append(Type type, AppendingContext appendingContext)
    {
        void AppendNamespace(string? ns)
        {
            if (ns == null)
                return;

            ILogStringNamespaceConfiguration namespaceConfiguration = appendingContext.VariableConfiguration;

            bool isImplicit = namespaceConfiguration.ImplicitNamespaces?.IsMatch(ns) ?? false;
            bool isExplicit = namespaceConfiguration.ExplicitNamespaces?.IsMatch(ns) ?? false;

            if ((isImplicit && isExplicit && namespaceConfiguration.IsNamespaceExplicitIfAmbiguous) ||
                (!isImplicit && !isExplicit && namespaceConfiguration.IsNamespaceExplicitIfUnspecified) ||
                isExplicit)
            {
                appendingContext
                    .AppendDirect(ns)
                    .AppendDirect('.');
            }
        }

        _ = appendingContext.MetaProperties.TryGetValue(CollectionLengthMetaProperty, out object? rawCollectionLength);

        using IDisposable _0 = appendingContext.WithMetaProperties(static x => { x.Remove(CollectionLengthMetaProperty); });

        if (type.IsArray)
        {
            Append(type.GetElementType()!, appendingContext);
            appendingContext.AppendDelimited(
                '[',
                ']',
                ac =>
                {
                    if (rawCollectionLength is int[] arrayLengths)
                    {
                        using IEnumerator<int> enumerator = arrayLengths.AsEnumerable().GetEnumerator();
                        ac.AppendEnumerator(
                            enumerator,
                            static (ac1, e) => { ac1.AppendDirect(e.Current.ToStringInvariant()); },
                            AllottingCounter.Unlimited,
                            ","
                        );
                    }
                    else
                    {
                        ac.AppendDirect(new string(',', type.GetArrayRank() - 1));
                    }
                }
            );
        }
        else if (type.IsPointer)
        {
            Append(type.GetElementType()!, appendingContext);
            appendingContext.AppendDirect('*');
        }
        else if (type.IsByRef)
        {
            Append(type.GetElementType()!, appendingContext);
            appendingContext.AppendDirect('&');
        }
        else if (type.IsNested)
        {
            Append(type.DeclaringType!, appendingContext);
            appendingContext
                .AppendDirect('+')
                .AppendDirect(type.Name);
        }
        else if (type.IsGenericParameter)
        {
            appendingContext.AppendDirect(type.Name);
        }
        else if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            Append(nullableType, appendingContext);
            appendingContext.AppendDirect('?');
        }
        else if (type.IsAnonymous())
        {
            appendingContext.AppendDirect('¤');
        }
        else if (IsValueTuple(type, out Type[]? itemTypes))
        {
            appendingContext.Append(LogStringTokens.TupleBegin);
            if (itemTypes.Length > 0)
            {
                Append(itemTypes[0], appendingContext);
                foreach (Type itemType in itemTypes.Skip(1))
                {
                    stringBuilder.Append(LogStringTokens.Separator);
                    Append(itemType, appendingContext);
                }
            }
            stringBuilder.Append(LogStringTokens.TupleEnd);
        }
        else if (type.IsGenericType)
        {
            AppendNamespace(type.Namespace);
            appendingContext
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                .AppendDirect(type.Name[..type.Name.IndexOf('`')])
#else
                .AppendDirect(type.Name.Substring(0, type.Name.IndexOf('`')))
#endif
                .AppendDelimited(
                    '<',
                    '>',
                    ac =>
                    {
                        if (type.IsGenericTypeDefinition)
                        {
                            ac.AppendDirect(new string(',', type.GetGenericArguments().Length - 1));
                        }
                        else
                        {
                            using IEnumerator<Type> enumerator = type.GetGenericArguments().AsEnumerable().GetEnumerator();
                            ac.AppendEnumerator(
                                enumerator,
                                (ac1, e) => { Append(e.Current!, ac1); },
                                AllottingCounter.Unlimited,
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
                appendingContext.AppendDirect(name);
            }
            else
            {
                AppendNamespace(type.Namespace);
                appendingContext.AppendDirect(type.Name);
            }
        }

        if (rawCollectionLength is int collectionLength)
        {
            appendingContext.AppendDirect('(').AppendDirect(collectionLength.ToString(CultureInfo.InvariantCulture)).AppendDirect(')');
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

    public void Append(ParameterInfo[] parameters, AppendingContext appendingContext)
    {
        appendingContext
            .AppendDelimited(
                '(',
                ')',
                ac =>
                {
                    using IEnumerator<ParameterInfo> enumerator = ((IReadOnlyList<ParameterInfo>)parameters).GetEnumerator();
                    ac.AppendEnumerator(
                        enumerator,
                        (ac1, e) => { Append(e.Current!, ac1); },
                        AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxMethodParameterCount())
                    );
                }
            );
    }

    private void Append(ParameterInfo parameter, AppendingContext appendingContext)
    {
        if (parameter.IsIn)
        {
            appendingContext.AppendDirect('↓');
        }
        if (parameter.IsOut)
        {
            appendingContext.AppendDirect('↑');
        }
        Append(parameter.ParameterType, appendingContext);
    }

    private sealed class LogStringableType : ILogStringable
    {
        private readonly Type type;
        private readonly MemberInfoLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        public LogStringableType(Type type, MemberInfoLogStringProvider owner)
        {
            this.type = type;
            this.owner = owner;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            owner.Append(type, appendingContext);
        }
    }

    private sealed class LogStringableMember : ILogStringable
    {
        private readonly MemberInfo member;
        private readonly MemberInfoLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        public LogStringableMember(MemberInfo member, MemberInfoLogStringProvider owner)
        {
            this.member = member;
            this.owner = owner;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            if (member.DeclaringType is { } declaringType)
            {
                owner.Append(declaringType, appendingContext);
                appendingContext.AppendDirect('#');
            }

            appendingContext.AppendDirect(member.Name);

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
                owner.Append(parameters, appendingContext);
            }
        }
    }

    private sealed class LogStringableParameter : ILogStringable
    {
        private readonly ParameterInfo parameter;
        private readonly MemberInfoLogStringProvider owner;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableParameter(ParameterInfo parameter, MemberInfoLogStringProvider owner)
        {
            this.parameter = parameter;
            this.owner = owner;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            owner.Append(parameter, appendingContext);
        }
    }

    private sealed class LogStringableAssembly : ILogStringable
    {
        private readonly Assembly assembly;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableAssembly(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect(assembly.FullName);
        }
    }
}
