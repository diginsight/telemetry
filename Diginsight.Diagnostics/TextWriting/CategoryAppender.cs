using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class CategoryAppender : IPrefixTokenAppender
{
    private readonly int length;

    public CategoryAppender(int? length)
    {
        this.length = length ?? 40;
    }

    public void Append(StringBuilder sb, LinePrefixData linePrefixData) => Append(sb, linePrefixData.Category);

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
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            finalCategory = $"…{category[^(length - 1)..]}";
#else
            finalCategory = $"…{category.Substring(category.Length - (length - 1))}";
#endif
        }
        else
        {
            finalCategory = category;
        }

        sb.Append(finalCategory);
    }
}
