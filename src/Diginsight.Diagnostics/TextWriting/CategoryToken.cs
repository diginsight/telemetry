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
        private readonly int length;

        public Appender(int? length)
        {
            this.length = length ?? 40;
        }

        public void Append(StringBuilder sb, in LinePrefixData linePrefixData) => Append(sb, linePrefixData.Category);

        private void Append(StringBuilder sb, string category)
        {
            if (length < 2)
            {
                throw new InvalidOperationException("Length must be greater than or equal to 2");
            }

            string finalCategory;
            if (category.Length < length)
            {
                finalCategory = category.PadRight(length);
            }
            else if (category.Length > length)
            {
                finalCategory = $"…{category[^(length - 1)..]}";
            }
            else
            {
                finalCategory = category;
            }

            sb.Append(finalCategory);
        }
    }
}
