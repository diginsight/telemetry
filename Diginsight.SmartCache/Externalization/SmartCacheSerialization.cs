using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Diginsight.SmartCache.Externalization;

public static class SmartCacheSerialization
{
    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(
        new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
            SerializationBinder = new MySerializationBinder(),
            Converters = new JsonConverter[]
            {
                new MyTypeConverter(),
                new MyEnumeratorConverter(),
            },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateParseHandling = DateParseHandling.None,
            DateFormatString = "O",
        }
    );

    public static readonly Encoding Encoding = new UTF8Encoding(false);

    public static byte[] SerializeToBytes(object? value, Type type)
    {
        using MemoryStream stream = new ();
        SerializeToStream(value, type, stream);
        return stream.ToArray();
    }

    public static byte[] SerializeToBytes<T>(T? value)
    {
        return SerializeToBytes(value, typeof(T));
    }

    public static void SerializeToStream(object? value, Type type, Stream stream)
    {
#if NET
        using TextWriter tw = new StreamWriter(stream, Encoding, leaveOpen: true);
#else
        using TextWriter tw = new StreamWriter(stream, Encoding, 1024, true);
#endif
        Serializer.Serialize(tw, value, type);
        tw.Flush();
    }

    public static void SerializeToStream<T>(T? value, Stream stream)
    {
        SerializeToStream(value, typeof(T), stream);
    }

    public static string SerializeToString(object? value, Type type)
    {
        using StringWriter sw = new ();
        Serializer.Serialize(sw, value, type);
        return sw.ToString();
    }

    public static string SerializeToString<T>(T? value)
    {
        return SerializeToString(value, typeof(T));
    }

    public static string SerializeType(Type type)
    {
        return JToken.FromObject(type, Serializer).ToObject<string>()!;
    }

    public static object? Deserialize(byte[] bytes, Type type)
    {
        using Stream stream = new MemoryStream(bytes, false);
        return Deserialize(stream, type);
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        return (T)Deserialize(bytes, typeof(T))!;
    }

    public static object? Deserialize(Stream stream, Type type)
    {
        using TextReader tr = new StreamReader(stream, Encoding);
        return Serializer.Deserialize(tr, type);
    }

    public static T Deserialize<T>(Stream stream)
    {
        return (T)Deserialize(stream, typeof(T))!;
    }

    public static object? Deserialize(string str, Type type)
    {
        using TextReader tr = new StringReader(str);
        return Serializer.Deserialize(tr, type);
    }

    public static T Deserialize<T>(string str)
    {
        return (T)Deserialize(str, typeof(T))!;
    }

    public static Type DeserializeType(string typeName)
    {
        return JToken.FromObject(typeName).ToObject<Type>(Serializer)!;
    }

    private sealed class MySerializationBinder : ISerializationBinder
    {
        private static readonly IReadOnlyDictionary<string, Assembly> NameToAssemblyMap;
        private static readonly IReadOnlyDictionary<Assembly, string> AssemblyToNameMap;
        private static readonly IReadOnlyDictionary<string, Type> NameToTypeMap;
        private static readonly IReadOnlyDictionary<Type, string> TypeToNameMap;

        private static readonly Regex TypeArgsRegex = new (
            """
            ^
            (?:
                (
                    (?:
                        [^\[\]]
                        |
                        (?<open>\[)
                        |
                        (?<-open>\])
                    )+?
                    (?(open)(?!))
                )
                (?:$|,\ *(?!$))
            )+
            $
            """,
            RegexOptions.IgnorePatternWhitespace
        );

        private static readonly Regex FullTypeRegex = new (
            """
            ^
            (
                (?:
                    [^\[\]]
                    |
                    (?<open>\[)
                    |
                    (?<-open>\])
                )+?
                (?(open)(?!))
            )
            (?:
                ,\ *
                (.+)
            )?
            $
            """,
            RegexOptions.IgnorePatternWhitespace
        );

        static MySerializationBinder()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            IEnumerable<ValueTuple<string, Assembly>> ownAssemblyPairs = assemblies
                .Where(static a => a.IsDefined(typeof(CacheInterchangeNameAttribute)))
                .Select(static a => (a.GetCustomAttribute<CacheInterchangeNameAttribute>()!.Name, a));

            IEnumerable<ValueTuple<string, Assembly>> externalAssemblyPairs = assemblies
                .SelectMany(static a => a.GetCustomAttributes<CacheInterchangeExternalNameAttribute>())
                .Where(static x => x.TargetAssembly is not null)
                .Select(
                    static x =>
                    {
                        try
                        {
                            return (x.Name, Assembly.Load(x.TargetAssembly!));
                        }
                        catch (Exception)
                        {
                            return (ValueTuple<string, Assembly>?)null;
                        }
                    }
                )
                .OfType<ValueTuple<string, Assembly>>();

            try
            {
                NameToAssemblyMap = ownAssemblyPairs.Concat(externalAssemblyPairs).ToDictionary(static x => x.Item1, static x => x.Item2);
                AssemblyToNameMap = NameToAssemblyMap.ToDictionary(static x => x.Value, static x => x.Key);
            }
            catch (ArgumentException)
            {
                NameToAssemblyMap = new Dictionary<string, Assembly>();
                AssemblyToNameMap = new Dictionary<Assembly, string>();
            }

            IEnumerable<ValueTuple<string, Type>> ownTypePairs = assemblies
                .SelectMany(
                    static a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            return Enumerable.Empty<Type>();
                        }
                    }
                )
                .Where(static t => t.IsDefined(typeof(CacheInterchangeNameAttribute)))
                .Select(static t => (t.GetCustomAttribute<CacheInterchangeNameAttribute>()!.Name, t));

            IEnumerable<ValueTuple<string, Type>> externalTypePairs = assemblies
                .SelectMany(static a => a.GetCustomAttributes<CacheInterchangeExternalNameAttribute>())
                .Where(static x => x.TargetType is not null)
                .Select(static x => (x.Name, x.TargetType!));

            try
            {
                NameToTypeMap = ownTypePairs.Concat(externalTypePairs).ToDictionary(static x => x.Item1, static x => x.Item2);
                TypeToNameMap = NameToTypeMap.ToDictionary(static x => x.Value, static x => x.Key);
            }
            catch (ArgumentException)
            {
                NameToTypeMap = new Dictionary<string, Type>();
                TypeToNameMap = new Dictionary<Type, string>();
            }
        }

        public Type BindToType(string? assemblyName, string typeName) => BindToType(assemblyName, typeName.AsSpan());

        public Type BindToType(string? assemblyName, ReadOnlySpan<char> typeName)
        {
            if (typeName.EndsWith("[]".AsSpan()))
            {
                return BindToType(assemblyName, typeName[..^2]).MakeArrayType();
            }

            if (typeName.IndexOf('#') < 0)
            {
                if (assemblyName is null)
                {
                    return Type.GetType(typeName.ToString())!;
                }

                Assembly assembly = assemblyName[0] == '#'
                    ? NameToAssemblyMap[assemblyName[1..]]
                    : Assembly.Load(assemblyName);

                return assembly.GetType(typeName.ToString())!;
            }

            int genericIndex = typeName.IndexOf('[');
            if (genericIndex < 0)
            {
                // By design, if typeName is not a constructed generic type and contains '#',
                // then it can only be in the form "#Name'; so we are done.
                return NameToTypeMap[typeName[1..].ToString()];
            }

            ReadOnlySpan<char> rootTypeName = typeName[..genericIndex];
            Type rootType = BindToType(assemblyName, rootTypeName);

            ReadOnlySpan<char> typeArgNames = typeName[(genericIndex + 1)..^1];
            Type[] typeArgs = TypeArgsRegex.Match(typeArgNames.ToString())
                .Groups[1]
                .Captures
#if !(NET || NETSTANDARD2_1_OR_GREATER)
                .Cast<Capture>()
#endif
                .Select(x => NestedBindToType(x.Value))
                .ToArray();

            return rootType.MakeGenericType(typeArgs);
        }

        private Type NestedBindToType(string fullTypeName)
        {
            return fullTypeName[0] == '['
                ? BindToType(this, fullTypeName[1..^1])
                : BindToType((string?)null, fullTypeName);
        }

        internal static Type BindToType(ISerializationBinder binder, string fullTypeName)
        {
            Match match = FullTypeRegex.Match(fullTypeName);
            return binder.BindToType(match.Groups[2] is { Success: true, Value: var assemblyName } ? assemblyName : null, match.Groups[1].Value);
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            if (serializedType.IsArray)
            {
                BindToName(serializedType.GetElementType()!, out assemblyName, out string? elementTypeName);
                typeName = $"{elementTypeName!}[]";
                return;
            }

            if (serializedType.IsConstructedGenericType)
            {
                BindToName(serializedType.GetGenericTypeDefinition(), out assemblyName, out string? rootTypeName);

                StringBuilder sb = new ();
#if NET || NETSTANDARD2_1_OR_GREATER
                sb.AppendJoin(",", serializedType.GetGenericArguments().Select(GetNestedBoundName));
#else
                using (IEnumerator<string> nbnEnumerator = serializedType.GetGenericArguments().Select(GetNestedBoundName).GetEnumerator())
                {
                    _ = nbnEnumerator.MoveNext();
                    sb.Append(nbnEnumerator.Current);

                    while (nbnEnumerator.MoveNext())
                    {
                        sb.Append(',');
                        sb.Append(nbnEnumerator.Current);
                    }
                }
#endif
                typeName = $"{rootTypeName}[{sb}]";

                return;
            }

            if (TypeToNameMap.TryGetValue(serializedType, out string? name0))
            {
                assemblyName = null;
                typeName = $"#{name0}";
                return;
            }

            if (AssemblyToNameMap.TryGetValue(serializedType.Assembly, out string? name1))
            {
                assemblyName = $"#{name1}";
                typeName = serializedType.FullName;
                return;
            }

            AssemblyName assemblyNameObj = serializedType.Assembly.GetName();
            assemblyName = assemblyNameObj.Name == "System.Private.CoreLib" ? null : assemblyNameObj.FullName;
            typeName = serializedType.FullName;
        }

        private string GetNestedBoundName(Type type)
        {
            BindToName(type, out string? assemblyName, out string? typeName);
            return assemblyName is null ? typeName! : $"[{typeName},{assemblyName}]";
        }
    }

    private sealed class MyTypeConverter : JsonConverter<Type>
    {
        public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            serializer.SerializationBinder.BindToName(value, out string? valueAssemblyName, out string? valueTypeName);
            serializer.Serialize(writer, valueAssemblyName != null ? $"{valueTypeName}, {valueAssemblyName}" : valueTypeName);
        }

        public override Type? ReadJson(JsonReader reader, Type objectType, Type? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string? fullTypeName = serializer.Deserialize<string?>(reader);
            return fullTypeName == null ? null : MySerializationBinder.BindToType(serializer.SerializationBinder, fullTypeName);
        }
    }

    private sealed class MyEnumeratorConverter : JsonConverter
    {
        private static readonly MethodInfo WriteJsonGenericMethod = typeof(MyEnumeratorConverter)
            .GetMethod(nameof(WriteJsonGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override bool CanWrite => true;
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return !new[] { typeof(IEnumerable<>), typeof(IEnumerator<>) }
                .Except(objectType.GetInterfaces().Where(static x => x.IsGenericType).Select(static x => x.GetGenericTypeDefinition()))
                .Any();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            Type type = value.GetType();
            Type elementType = type.GetInterfaces()
                .First(static x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .GetGenericArguments()[0];

            WriteJsonGenericMethod
                .MakeGenericMethod(elementType)
                .Invoke(this, [ writer, value, serializer ]);
        }

        private void WriteJsonGeneric<T>(JsonWriter writer, IEnumerable<T> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToArray(), typeof(IEnumerable<T>));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
