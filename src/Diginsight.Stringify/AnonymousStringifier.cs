using System.Reflection;

namespace Diginsight.Stringify;

internal sealed class AnonymousStringifier : ReflectionStringifier
{
    private readonly IReflectionStringifyHelper helper;

    public AnonymousStringifier(IReflectionStringifyHelper helper)
    {
        this.helper = helper;
    }

    protected override Handling IsHandled(Type type) => type.IsAnonymous() ? Handling.Handle : Handling.Pass;

    protected override ReflectionStringifiable MakeStringifiable(object obj) => new AnonymousStringifiable(obj, helper);

    private sealed class AnonymousStringifiable : ReflectionStringifiable
    {
        public AnonymousStringifiable(object obj, IReflectionStringifyHelper helper)
            : base(obj, helper) { }

        protected override StringifyAppender[] MakeAppenders(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => MakeAppender(x, null, null))
                .ToArray();
        }

        protected override AllottedCounter Count(StringifyContext stringifyContext)
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxAnonymousObjectPropertyCount());
        }
    }
}
