namespace Diginsight.Stringify;

/// <summary>
/// Provides token constants used in compact string representations.
/// </summary>
public static class StringifyTokens
{
    /// <summary>
    /// The token that begins a collection.
    /// </summary>
    public const char CollectionBegin = '[';
    /// <summary>
    /// The token that ends a collection.
    /// </summary>
    public const char CollectionEnd = ']';
    /// <summary>
    /// The token that represents a value omitted because the maximum depth was reached.
    /// </summary>
    public const char Deep = '»';
    /// <summary>
    /// The token that represents omitted content.
    /// </summary>
    public const char Ellipsis = '…';
    /// <summary>
    /// The token that represents an error while appending a value.
    /// </summary>
    public const char Error = '%';
    /// <summary>
    /// The token that begins a literal value.
    /// </summary>
    public const char LiteralBegin = '$';
    /// <summary>
    /// The token that ends a literal value.
    /// </summary>
    public const char LiteralEnd = '$';
    /// <summary>
    /// The token that begins a map.
    /// </summary>
    public const char MapBegin = '{';
    /// <summary>
    /// The token that ends a map.
    /// </summary>
    public const char MapEnd = '}';
    internal const char Null = '□';
    /// <summary>
    /// The compact separator token.
    /// </summary>
    public const char Separator = ',';
    /// <summary>
    /// The compact separator string.
    /// </summary>
    public const string Separator1 = ",";
    /// <summary>
    /// The spaced separator string.
    /// </summary>
    public const string Separator2 = ", ";
    /// <summary>
    /// The token that begins a tuple.
    /// </summary>
    public const char TupleBegin = '(';
    /// <summary>
    /// The token that ends a tuple.
    /// </summary>
    public const char TupleEnd = ')';
    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    public const char Value = ':';
}
