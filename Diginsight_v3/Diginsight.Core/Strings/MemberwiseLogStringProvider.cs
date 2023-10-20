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
        bool directLoggable = type.IsDefined(typeof(LoggableObjectAttribute), false);
        bool directNonLoggable = type.IsDefined(typeof(NonLoggableObjectAttribute), false);

        return logStringConfiguration.IsMemberwiseLoggableByDefault
            ? !(directNonLoggable || (type.IsDefined(typeof(NonLoggableObjectAttribute), true) && !directLoggable))
            : directLoggable || (type.IsDefined(typeof(LoggableObjectAttribute), true) && !directNonLoggable);
    }

    protected override Action<object, StringBuilder, LoggingContext>[] MakeAppenders(Type type)
    {
        var fieldAppenders = type.GetFields()
            .Where(
                static f => !f.IsDefined(typeof(NonLoggableMemberAttribute))
                    && !f.FieldType.IsForbidden()
            )
            .Select(static f => (f, a: f.GetCustomAttribute<LoggableMemberAttribute>()))
            .Where(IsIncluded)
            .Select(x => MakeAppender(x.a?.Name, x.a?.Provider, x.f));
        var propertyAppenders = type.GetProperties()
            .Where(
                static p => p.GetIndexParameters().Length == 0
                    && !p.IsDefined(typeof(NonLoggableMemberAttribute))
                    && !p.PropertyType.IsForbidden()
            )
            .Select(static p => (p, a: p.GetCustomAttribute<LoggableMemberAttribute>()))
            .Where(IsIncluded)
            .Select(x => MakeAppender(x.a?.Name, x.a?.Provider, x.p));
        return fieldAppenders.Concat(propertyAppenders).ToArray();
    }

    private static bool IsIncluded((FieldInfo, LoggableMemberAttribute?) pair)
    {
        (FieldInfo field, LoggableMemberAttribute? attribute) = pair;
        return attribute is not null || field.IsPublic;
    }

    private static bool IsIncluded((PropertyInfo, LoggableMemberAttribute?) pair)
    {
        (PropertyInfo property, LoggableMemberAttribute? attribute) = pair;
        if (property.GetMethod is not { } method)
            return false;
        return attribute is not null || method.IsPublic;
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountMemberwiseProperties();
}
