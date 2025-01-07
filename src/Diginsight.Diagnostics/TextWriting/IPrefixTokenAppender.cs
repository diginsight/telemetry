using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public interface IPrefixTokenAppender
{
    void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor);
}
