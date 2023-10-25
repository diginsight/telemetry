using System.Reflection;

namespace Diginsight.Strings;

internal sealed class AnonymousLogStringProvider : ReflectionLogStringProvider
{
    private readonly IReflectionLogStringHelper helper;

    public AnonymousLogStringProvider(IReflectionLogStringHelper helper)
    {
        this.helper = helper;
    }

    protected override Handling IsHandled(Type type) => type.IsAnonymous() ? Handling.Handle : Handling.Pass;

    protected override ReflectionLogStringable MakeLogStringable(object obj) => new AnonymousLogStringable(obj, helper);

    private sealed class AnonymousLogStringable : ReflectionLogStringable
    {
        public AnonymousLogStringable(object obj, IReflectionLogStringHelper helper)
            : base(obj, helper) { }

        protected override LogStringAppender[] MakeAppenders(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => MakeAppender(null, null, x))
                .ToArray();
        }

        protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountAnonymousObjectProperties();
    }
}
