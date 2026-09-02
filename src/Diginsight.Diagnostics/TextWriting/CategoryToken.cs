using Pastel;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends the log category to the line prefix.
/// </summary>
public sealed class CategoryToken : ILineToken
{
    /// <summary>
    /// Gets the desired category length.
    /// </summary>
    public int? Length { get; set; }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Length));
    }

    /// <inheritdoc />
    public ILineToken Clone() => new CategoryToken() { Length = Length };

    private sealed class Appender : IPrefixTokenAppender
    {
        private readonly int desiredLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="Appender" /> class with a desired category length.
        /// </summary>
        /// <param name="desiredLength">The desired category length.</param>
        public Appender(int? desiredLength)
        {
            this.desiredLength = desiredLength ?? 40;
        }

        /// <inheritdoc />
        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            Append(sb, linePrefixData.Category, useColor);
            length += desiredLength;
        }

        private void Append(StringBuilder sb, string category, bool useColor)
        {
            if (desiredLength < 2)
            {
                throw new InvalidOperationException("Length must be greater than or equal to 2");
            }

            string finalCategory;
            if (category.Length < desiredLength)
            {
                finalCategory = category.PadRight(desiredLength);
            }
            else if (category.Length > desiredLength)
            {
                finalCategory = $"…{category[^(desiredLength - 1)..]}";
            }
            else
            {
                finalCategory = category;
            }

            sb.Append(useColor ? finalCategory.Pastel(ConsoleColor.White) : finalCategory);
        }
    }
}
