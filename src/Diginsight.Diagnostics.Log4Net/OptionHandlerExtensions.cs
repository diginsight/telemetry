using log4net.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.Log4Net;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class OptionHandlerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsActivated<T>(this T optionHandler)
        where T : IOptionHandler
    {
        optionHandler.ActivateOptions();
        return optionHandler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsActivatedOptionHandler<T>(this T obj)
    {
        (obj as IOptionHandler)?.ActivateOptions();
        return obj;
    }
}
