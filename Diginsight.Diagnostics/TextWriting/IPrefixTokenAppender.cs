using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public interface IPrefixTokenAppender
{
    void Append(StringBuilder sb, in LinePrefixData linePrefixData);
}
