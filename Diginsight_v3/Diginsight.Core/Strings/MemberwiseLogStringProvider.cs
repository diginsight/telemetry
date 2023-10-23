using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal sealed class MemberwiseLogStringProvider : ReflectionLogStringProvider
{
    private readonly ILogStringConfiguration logStringConfiguration;

    public MemberwiseLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider,
        IServiceProvider serviceProvider,
        IOptions<LogStringConfiguration> logStringConfigurationOptions
    )
        : base(memberLogStringProvider, serviceProvider)
    {
        logStringConfiguration = logStringConfigurationOptions.Value;
    }

    protected override bool IsHandled(Type type)
    {
        bool directLogStringable = type.IsDefined(typeof(LogStringableObjectAttribute), false);
        bool directNonLogStringable = type.IsDefined(typeof(NonLogStringableObjectAttribute), false);

        return logStringConfiguration.IsMemberwiseLogStringableByDefault
            ? !(directNonLogStringable || (type.IsDefined(typeof(NonLogStringableObjectAttribute), true) && !directLogStringable))
            : directLogStringable || (type.IsDefined(typeof(LogStringableObjectAttribute), true) && !directNonLogStringable);
    }

    protected override Action<object, StringBuilder, LoggingContext>[] MakeAppenders(Type type)
    {
        var fieldAppenders = type.GetFields()
            .Where(
                static f => !f.IsDefined(typeof(NonLogStringableMemberAttribute))
                    && !f.FieldType.IsForbidden()
            )
            .Select(static f => (f, a: f.GetCustomAttribute<LogStringableMemberAttribute>()))
            .Where(IsIncluded)
            .Select(x => MakeAppender(x.a?.Name, x.a?.Provider, x.f));
        var propertyAppenders = type.GetProperties()
            .Where(
                static p => p.GetIndexParameters().Length == 0
                    && !p.IsDefined(typeof(NonLogStringableMemberAttribute))
                    && !p.PropertyType.IsForbidden()
            )
            .Select(static p => (p, a: p.GetCustomAttribute<LogStringableMemberAttribute>()))
            .Where(IsIncluded)
            .Select(x => MakeAppender(x.a?.Name, x.a?.Provider, x.p));
        return fieldAppenders.Concat(propertyAppenders).ToArray();
    }

    private static bool IsIncluded((FieldInfo, LogStringableMemberAttribute?) pair)
    {
        (FieldInfo field, LogStringableMemberAttribute? attribute) = pair;
        return attribute is not null || field.IsPublic;
    }

    private static bool IsIncluded((PropertyInfo, LogStringableMemberAttribute?) pair)
    {
        (PropertyInfo property, LogStringableMemberAttribute? attribute) = pair;
        if (property.GetMethod is not { } method)
            return false;
        return attribute is not null || method.IsPublic;
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountMemberwiseProperties();
}
