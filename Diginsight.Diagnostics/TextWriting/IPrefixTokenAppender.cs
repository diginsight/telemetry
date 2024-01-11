using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal interface IPrefixTokenAppender
{
    void Append(StringBuilder sb, LinePrefixData linePrefixData);
}
