using System;

namespace Common
{
    public interface IClassConfigurationGetterProvider
    {
        IClassConfigurationGetterProvider Empty { get; }
        IClassConfigurationGetter GetFor(Type @class);
    }

    internal sealed class EmptyClassConfigurationGetterProvider : IClassConfigurationGetterProvider
    {
        internal static IClassConfigurationGetterProvider _empty = new EmptyClassConfigurationGetterProvider();
        IClassConfigurationGetterProvider Empty => _empty;
        IClassConfigurationGetterProvider IClassConfigurationGetterProvider.Empty => _empty;

        public IClassConfigurationGetter GetFor(Type @class) => EmptyClassConfigurationGetter._empty;
    }
}

