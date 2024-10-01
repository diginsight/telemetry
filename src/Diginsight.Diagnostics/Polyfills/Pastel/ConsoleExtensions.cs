#if !NET
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Pastel;

internal static class ConsoleExtensions
{
    public static void Enable() { }

    public static string Pastel(this string str, [SuppressMessage("ReSharper", "UnusedParameter.Global")] ConsoleColor color) => str;

    public static string PastelBg(this string str, [SuppressMessage("ReSharper", "UnusedParameter.Global")] ConsoleColor color) => str;
}
#endif
