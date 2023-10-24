using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal sealed class AnonymousLogStringProvider : ReflectionLogStringProvider
{
    public AnonymousLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider,
        IServiceProvider serviceProvider
    )
        : base(memberLogStringProvider, serviceProvider) { }

    protected override Handling IsHandled(Type type) => type.IsAnonymous() ? Handling.Handle : Handling.Pass;

    protected override Action<object, StringBuilder, LoggingContext>[] MakeAppenders(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => MakeAppender(null, null, x))
            .ToArray();
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountAnonymousObjectProperties();
}
