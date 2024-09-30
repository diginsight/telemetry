using System.Reflection;

namespace Diginsight.Stringify;

internal interface IMemberInfoStringifier : IStringifier
{
    void Append(Type type, StringifyContext stringifyContext);

    void Append(ParameterInfo[] parameters, StringifyContext stringifyContext);
}
