using log4net.Core;

namespace Diginsight.Diagnostics.Log4Net;

public static class OptionHandlerExtensions
{
    public static T AsActivated<T>(this T optionHandler)
        where T : IOptionHandler
    {
        optionHandler.ActivateOptions();
        return optionHandler;
    }

    public static T AsActivatedOptionHandler<T>(this T obj)
    {
        (obj as IOptionHandler)?.ActivateOptions();
        return obj;
    }
}
