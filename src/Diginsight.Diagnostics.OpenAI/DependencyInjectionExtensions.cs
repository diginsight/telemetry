using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static IOpenAIBuilder AddDiginsightOpenAI(this IServiceCollection services)
    //{
    //    services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, EnsureOpenAI>());

    //    //IOpenAIBuilder OpenAIBuilder = services.AddOpenAI();

    //    //OpenAIBuilder
    //    //    .ConfigureResource(
    //    //        static resourceBuilder =>
    //    //        {
    //    //            resourceBuilder.AddService(
    //    //                Assembly.GetEntryAssembly()!.GetName().Name ?? throw new UnreachableException("Entry assembly is not present or unnamed"),
    //    //                serviceInstanceId: Environment.MachineName
    //    //            );
    //    //        }
    //    //    );

    //    return OpenAIBuilder;
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddDiginsightOpenAI(this ILoggingBuilder loggingBuilder) // , Action<OpenAILoggerOptions>? configure = null
    {
        //return loggingBuilder
        //    .AddDiginsightCore()
        //    .AddOpenAI(
        //        OpenAILoggerOptions =>
        //        {
        //            OpenAILoggerOptions.IncludeFormattedMessage = true;
        //            OpenAILoggerOptions.IncludeScopes = true;
        //            configure?.Invoke(OpenAILoggerOptions);
        //        }
        //    );
        return null!;
    }

}
