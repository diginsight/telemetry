namespace Diginsight;

public interface IClassConfigurationGetter
{
    T? Get<T>(string key, T? defaultValue = default);
}

// ReSharper disable once UnusedTypeParameter
public interface IClassConfigurationGetter<TClass> : IClassConfigurationGetter { }
