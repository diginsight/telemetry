using System.Reflection;
using System.Text;

namespace Diginsight.Stringify;

internal interface IMemberStringifier : IStringifier
{
    void Append(Type type, StringBuilder stringBuilder, StringifyContext stringifyContext);

    void Append(ParameterInfo[] parameters, StringBuilder stringBuilder, StringifyContext stringifyContext);
}
