namespace Diginsight.Options;

/// <summary>
/// Represents an interface for volatilely configurable option objects.
/// </summary>
/// <remarks>
/// Volatile and dynamic configuration are features that allow the options system to overwrite specific configuration data at runtime.
/// While volatile configuration is persisted across scopes, dynamic configuration only lasts the lifetime of a scope.
/// With the term "scope" we mean a specific context, such as an HTTP request in a web API.
/// </remarks>
public interface IVolatilelyConfigurable
{
    /// <summary>
    /// Invoked by the options system to fill this instance with volatile configuration.
    /// </summary>
    /// <remarks>
    ///     <para>Implementors should return either the object itself, or a wrapper that "masks" the properties available for volatile configuration.</para>
    ///     <para>When a wrapper is returned, this can be a private nested class.</para>
    ///     <para>In .NET Standard 2.1 and .NET 5+, the default implementation returns the object itself.</para>
    /// </remarks>
    /// <returns>An object that serves as a filler.</returns>
    /// <example>
    /// Implementation with custom filler:
    /// <code>
    ///         public class MyOptions : IVolatilelyConfigurable
    ///         {
    ///             public string? Foo { get; set; }
    /// 
    ///             public double Baz { get; set; }
    /// 
    ///             public ICollection&lt;string&gt; Bars { get; private set; } = new List&lt;string&gt;();
    /// 
    ///             object IVolatilelyConfigurable.MakeFiller() => new Filler(this);
    /// 
    ///             private class Filler
    ///             {
    ///                 private readonly MyOptions filled;
    /// 
    ///                 public Filler(MyOptions filled) => this.filled = filled;
    /// 
    ///                 public string? Foo
    ///                 {
    ///                     get => filled.Foo;
    ///                     set => filled.Foo = value;
    ///                 }
    /// 
    ///                 public string Bar
    ///                 {
    ///                     get => string.Join(';', filled.Bars);
    ///                     set => filled.Bars = value.Split(';');
    ///                 }
    ///             }
    ///         }
    ///     </code>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Property <c>Foo</c> is untouched.</description>
    ///     </item>
    ///     <item>
    ///         <description>Property <c>Baz</c> is not exposed.</description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Property <c>Bars</c> is exposed with different name (<c>Bar</c>) and type;
    ///         conversion logic is in the corresponding property in the filler.
    ///         </description>
    ///     </item>
    /// </list>
    /// </example>
    object MakeFiller()
#if NET || NETSTANDARD2_1_OR_GREATER
        => this;
#else
        ;
#endif
}
