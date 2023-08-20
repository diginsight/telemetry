using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common
{
    /// <summary>
    /// Extension class.
    /// </summary>
    public static class AddParallelServiceExtension
    {
        /// <summary>
        /// Extension method for registering cache related services.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static IServiceCollection AddParallelService(this IServiceCollection services, IConfiguration configuration)
        {
            //services.Configure<CacheServiceOptions>(configuration.GetSection(nameof(CacheServiceOptions)));
            //services.AddSingleton((serviceProvider) =>
            //{
            //    var options = serviceProvider.GetRequiredService<IOptions<CacheServiceOptions>>();
            //    return new MemoryCache(new MemoryCacheOptions() { SizeLimit = options.Value.SizeLimit * 1024 * 1024 });
            //});
            //services.AddSingleton<IEnergyManagerMemoryCache, EnergyManagerMemoryCache>();
            services.AddSingleton<IParallelService, ParallelService>();

            return services;
        }
    }
}
