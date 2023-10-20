using System.Text;

namespace Diginsight.Strings;

internal sealed class AnonymousLogStringProvider : ReflectionLogStringProvider
{
    public AnonymousLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider,
        IServiceProvider serviceProvider
    )
        : base(memberLogStringProvider, serviceProvider) { }

    protected override bool IsHandled(Type type) => type.IsAnonymous();

    protected override Action<object, StringBuilder, LoggingContext>[] MakeAppenders(Type type)
    {
        return type.GetProperties().Select(x => MakeAppender(null, null, x)).ToArray();
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountAnonymousObjectProperties();
}
