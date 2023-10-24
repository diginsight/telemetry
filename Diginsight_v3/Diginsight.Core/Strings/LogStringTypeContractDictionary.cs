namespace Diginsight.Strings;

public sealed class LogStringTypeContractDictionary : Dictionary<Type, LogStringTypeContract>
{
    public LogStringTypeContractDictionary()
    {
        this[typeof(Exception)] = LogStringTypeContract.For(
            typeof(Exception),
            static tc => tc
                .ForMember(nameof(Exception.TargetSite), static mc => mc.SetIncluded(false))
                .ForMember(nameof(Exception.HelpLink), static mc => mc.SetIncluded(false))
                .ForMember(nameof(Exception.Data), static mc => mc.SetIncluded(false))
        );
    }
}
