using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal sealed class MemberLogStringProvider : IMemberLogStringProvider
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

    private readonly ILogStringConfiguration logStringConfiguration;

    public MemberLogStringProvider(
        IOptions<LogStringConfiguration> logStringConfigurationOptions
    )
    {
        logStringConfiguration = logStringConfigurationOptions.Value;
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

    public void Append(Type type, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        void AppendNamespace(string? ns)
        {
            if (ns == null)
                return;

            bool isImplicit = logStringConfiguration.ImplicitNamespaces?.IsMatch(ns) ?? false;
            bool isExplicit = logStringConfiguration.ExplicitNamespaces?.IsMatch(ns) ?? false;

            if ((isImplicit && isExplicit && logStringConfiguration.IsNamespaceExplicitIfAmbiguous) ||
                (!isImplicit && !isExplicit && logStringConfiguration.IsNamespaceExplicitIfUnspecified) ||
                isExplicit)
            {
                stringBuilder
                    .Append(ns)
                    .Append('.');
            }
        }

        _ = loggingContext.MetaProperties.TryGetValue(CollectionLengthMetaProperty, out object? rawCollectionLength);

        using IDisposable _0 = loggingContext.WithMetaProperties(static x => { x.Remove(CollectionLengthMetaProperty); });

        if (type.IsArray)
        {
            Append(type.GetElementType()!, stringBuilder, loggingContext);

            stringBuilder.Append("[");

            if (rawCollectionLength is int[] arrayLengths)
            {
                for (int i = 0; i < arrayLengths.Length; i++)
                {
                    if (i > 0)
                        stringBuilder.Append(',');
                    stringBuilder.Append(arrayLengths[i].ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                stringBuilder.Append(new string(',', type.GetArrayRank() - 1));
            }

            stringBuilder.Append("]");
        }
        else if (type.IsPointer)
        {
            Append(type.GetElementType()!, stringBuilder, loggingContext);
            stringBuilder.Append('*');
        }
        else if (type.IsByRef)
        {
            Append(type.GetElementType()!, stringBuilder, loggingContext);
            stringBuilder.Append('&');
        }
        else if (type.IsNested)
        {
            Append(type.DeclaringType!, stringBuilder, loggingContext);
            stringBuilder
                .Append('+')
                .Append(type.Name);
        }
        else if (type.IsGenericParameter)
        {
            stringBuilder.Append(type.Name);
        }
        else if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            Append(nullableType, stringBuilder, loggingContext);
            stringBuilder.Append('?');
        }
        else if (type.IsAnonymous())
        {
            stringBuilder.Append('¤');
        }
        else if (IsValueTuple(type, out Type[]? itemTypes))
        {
            stringBuilder.Append(LogStringTokens.TupleBegin);
            if (itemTypes.Length > 0)
            {
                Append(itemTypes[0], stringBuilder, loggingContext);
                foreach (Type itemType in itemTypes.Skip(1))
                {
                    stringBuilder.Append(LogStringTokens.Separator);
                    Append(itemType, stringBuilder, loggingContext);
                }
            }
            stringBuilder.Append(LogStringTokens.TupleEnd);
        }
        else if (type.IsGenericType)
        {
            AppendNamespace(type.Namespace);
            stringBuilder
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                .Append(type.Name[..type.Name.IndexOf('`')])
#else
                .Append(type.Name.Substring(0, type.Name.IndexOf('`')))
#endif
                .Append('<');
            if (type.IsGenericTypeDefinition)
            {
                stringBuilder.Append(new string(LogStringTokens.Separator, type.GetGenericArguments().Length - 1));
            }
            else
            {
                Type[] typeArgs = type.GetGenericArguments();
                Append(typeArgs[0], stringBuilder, loggingContext);
                foreach (Type typeArg in typeArgs.Skip(1))
                {
                    stringBuilder.Append(LogStringTokens.Separator);
                    Append(typeArg, stringBuilder, loggingContext);
                }
            }
            stringBuilder.Append('>');
        }
        else
        {
            if (logStringConfiguration.ShortenKnownTypes && KNOWN_TYPE_NAMES.TryGetValue(type, out string? name))
            {
                stringBuilder.Append(name);
            }
            else
            {
                AppendNamespace(type.Namespace);
                stringBuilder.Append(type.Name);
            }
        }

        if (rawCollectionLength is int collectionLength)
        {
            stringBuilder.Append('(').Append(collectionLength.ToString(CultureInfo.InvariantCulture)).Append(')');
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

    public void Append(ParameterInfo[] parameters, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        stringBuilder.Append('(');
        AppendCore(parameters, stringBuilder, loggingContext);
        stringBuilder.Append(')');
    }

    private void AppendCore(IReadOnlyList<ParameterInfo> parameters, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        if (parameters.Count <= 0)
            return;

        AllottingCounter counter = loggingContext.CountMethodParameters();

        try
        {
            void AppendItem(int i)
            {
                counter.Decrement();
                Append(parameters[i], stringBuilder, loggingContext);
            }

            AppendItem(0);
            for (int i = 1; i < parameters.Count; i++)
            {
                stringBuilder.Append(LogStringTokens.Separator2);
                AppendItem(i);
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
        }
    }

    private void Append(ParameterInfo parameter, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        if (parameter.IsIn)
        {
            stringBuilder.Append('↓');
        }
        if (parameter.IsOut)
        {
            stringBuilder.Append('↑');
        }
        Append(parameter.ParameterType, stringBuilder, loggingContext);
    }

    private sealed class LogStringableType : ILogStringable
    {
        private readonly Type type;
        private readonly MemberLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        public LogStringableType(Type type, MemberLogStringProvider owner)
        {
            this.type = type;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            owner.Append(type, stringBuilder, loggingContext);
        }
    }

    private sealed class LogStringableMember : ILogStringable
    {
        private readonly MemberInfo member;
        private readonly MemberLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        public LogStringableMember(MemberInfo member, MemberLogStringProvider owner)
        {
            this.member = member;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            if (member.DeclaringType is { } declaringType)
            {
                owner.Append(declaringType, stringBuilder, loggingContext);
                stringBuilder.Append('#');
            }

            stringBuilder.Append(member.Name);

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
                owner.Append(parameters, stringBuilder, loggingContext);
            }
        }
    }

    private sealed class LogStringableParameter : ILogStringable
    {
        private readonly ParameterInfo parameter;
        private readonly MemberLogStringProvider owner;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableParameter(ParameterInfo parameter, MemberLogStringProvider owner)
        {
            this.parameter = parameter;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            owner.Append(parameter, stringBuilder, loggingContext);
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

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append(assembly.FullName);
        }
    }
}
