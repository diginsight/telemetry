using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal interface IMemberLogStringProvider : ILogStringProvider
{
    void Append(Type type, StringBuilder stringBuilder, AppendingContext appendingContext);

    void Append(ParameterInfo[] parameters, StringBuilder stringBuilder, AppendingContext appendingContext);
}
