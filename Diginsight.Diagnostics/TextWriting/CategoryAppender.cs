using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class CategoryAppender : IPrefixTokenAppender
{
    private readonly int categoryLength;

    public CategoryAppender(int categoryLength)
    {
        this.categoryLength = categoryLength;
    }

    public void Append(StringBuilder sb, string category)
    {
        // TODO Move this logic outside
        if (categoryLength < 2)
        {
            return;
        }

        string finalCategory;
        if (category.Length < categoryLength)
        {
            finalCategory = category.PadRight(categoryLength);
        }
        else if (category.Length > categoryLength)
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            finalCategory = $"…{category[^(categoryLength - 1)..]}";
#else
            finalCategory = $"…{category.Substring(category.Length - (categoryLength - 1))}";
#endif
        }
        else
        {
            finalCategory = category;
        }

        sb.Append(finalCategory);
    }
}
