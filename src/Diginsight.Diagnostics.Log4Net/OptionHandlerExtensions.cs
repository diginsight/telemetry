using log4net.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.Log4Net;

/// <summary>
/// Provides extension methods for activating Log4Net option handlers.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class OptionHandlerExtensions
{
    /// <summary>
    /// Activates the option handler and returns it.
    /// </summary>
    /// <typeparam name="T">The option handler type.</typeparam>
    /// <param name="optionHandler">The option handler to activate.</param>
    /// <returns>The activated option handler.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsActivated<T>(this T optionHandler)
        where T : IOptionHandler
    {
        optionHandler.ActivateOptions();
        return optionHandler;
    }

    /// <summary>
    /// Activates the object when it implements <see cref="IOptionHandler" /> and returns it.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="obj">The object to activate when possible.</param>
    /// <returns>The activated object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsActivatedOptionHandler<T>(this T obj)
    {
        (obj as IOptionHandler)?.ActivateOptions();
        return obj;
    }
}
