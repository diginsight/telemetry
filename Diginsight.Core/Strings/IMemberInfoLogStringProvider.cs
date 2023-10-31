using System.Reflection;

namespace Diginsight.Strings;

internal interface IMemberInfoLogStringProvider : ILogStringProvider
{
    void Append(Type type, AppendingContext appendingContext);

    void Append(ParameterInfo[] parameters, AppendingContext appendingContext);
}
