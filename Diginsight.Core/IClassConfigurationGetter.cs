namespace Diginsight;

public interface IClassConfigurationGetter
{
    IEnumerable<T> GetAll<T>(string key, SafeConverter<T>? tryConvert = null);

    bool TryGet<T>(string key, out T value, SafeConverter<T>? tryConvert = null);

    delegate bool SafeConverter<T>(string? rawValue, out T value);
}

// ReSharper disable once UnusedTypeParameter
public interface IClassConfigurationGetter<TClass> : IClassConfigurationGetter;
