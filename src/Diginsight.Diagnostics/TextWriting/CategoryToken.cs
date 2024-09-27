using Pastel;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class CategoryToken : ILineToken
{
    public int? Length { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Length));
    }

    public ILineToken Clone() => new CategoryToken() { Length = Length };

    private sealed class Appender : IPrefixTokenAppender
    {
        private readonly int desiredLength;

        public Appender(int? desiredLength)
        {
            this.desiredLength = desiredLength ?? 40;
        }

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
