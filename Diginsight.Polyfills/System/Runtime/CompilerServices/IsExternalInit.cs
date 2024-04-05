#if !NET6_0_OR_GREATER
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit;
#endif
