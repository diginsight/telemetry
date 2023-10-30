using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

public static class AppendingContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable? IncrementDepth(this AppendingContext appendingContext, bool condition, out bool isMaxDepth)
    {
        if (condition)
        {
            return appendingContext.IncrementDepth(out isMaxDepth);
        }
        else
        {
            isMaxDepth = false;
            return null;
        }
    }

    public static AllottingCounter CountCollectionItems(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxCollectionItemCount());
    }

    public static AllottingCounter CountDictionaryItems(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxDictionaryItemCount());
    }

    public static AllottingCounter CountMemberwiseProperties(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxMemberwisePropertyCount());
    }
}
