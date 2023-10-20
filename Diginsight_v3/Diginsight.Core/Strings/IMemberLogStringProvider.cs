using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal interface IMemberLogStringProvider : ILogStringProvider
{
    void Append(Type type, StringBuilder stringBuilder, LoggingContext loggingContext);

    void Append(ParameterInfo[] parameters, StringBuilder stringBuilder, LoggingContext loggingContext);
}
