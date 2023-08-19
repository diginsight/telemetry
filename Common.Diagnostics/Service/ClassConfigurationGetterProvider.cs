//#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Common
{
    public sealed class ClassConfigurationGetterProvider : IClassConfigurationGetterProvider
    {
        private readonly IServiceProvider serviceProvider;
        public IClassConfigurationGetterProvider Empty => EmptyClassConfigurationGetterProvider._empty;

        public ClassConfigurationGetterProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IClassConfigurationGetter GetFor(Type @class)
        {
            return (IClassConfigurationGetter)serviceProvider.GetRequiredService(typeof(IClassConfigurationGetter<>).MakeGenericType(@class));
        }
    }
}

