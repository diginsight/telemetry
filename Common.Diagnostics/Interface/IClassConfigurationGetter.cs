namespace Common
{
    public interface IClassConfigurationGetter
    {
        IClassConfigurationGetter Empty { get; }
        T Get<T>(string key, T defaultValue = default);
    }
    public sealed class EmptyClassConfigurationGetter : IClassConfigurationGetter
    {
        internal static IClassConfigurationGetter _empty = new EmptyClassConfigurationGetter();
        IClassConfigurationGetter IClassConfigurationGetter.Empty => _empty;

        public T Get<T>(string key, T defaultValue = default) => defaultValue;
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IClassConfigurationGetter<TClass> : IClassConfigurationGetter
    {
        IClassConfigurationGetter<TClass> Empty { get; }
    }
    public sealed class EmptyClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
    {
        internal static IClassConfigurationGetter<TClass> _empty = new EmptyClassConfigurationGetter<TClass>();
        public IClassConfigurationGetter<TClass> Empty => _empty;
        IClassConfigurationGetter IClassConfigurationGetter.Empty => _empty;

        T IClassConfigurationGetter.Get<T>(string key, T defaultValue) { return _empty.Get(key, defaultValue); }
    }
}

