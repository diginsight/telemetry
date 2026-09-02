using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for creating stringify contexts and stringifiable values.
/// </summary>
public interface IStringifyContextFactory
{
    /// <summary>
    /// Creates a stringify context.
    /// </summary>
    /// <param name="stringBuilder">When this method returns, contains the string builder used by the context.</param>
    /// <returns>The created stringify context.</returns>
    StringifyContext MakeStringifyContext([NotNull] ref StringBuilder? stringBuilder);

    /// <summary>
    /// Creates a builder initialized from the current factory.
    /// </summary>
    /// <returns>A builder initialized with the same configuration.</returns>
    StringifyContextFactoryBuilder PrepareClone();

    /// <summary>
    /// Converts the specified object to a stringifiable representation.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <returns>The stringifiable representation.</returns>
    IStringifiable ToStringifiable(object? obj);
}
