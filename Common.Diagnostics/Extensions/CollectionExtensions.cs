#region using
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace Common
{
    #region TraverseOrder
    internal enum TraverseOrder
    {
        InOrder = 0,
        PreOrder = 1,
        PostOrder = 2
    }
    #endregion
    #region TraverseOptions
    internal enum TraverseOptions
    {
        All = 0,
        NodesOnly = 1,
        LeavesOnly = 2
    }
    #endregion
    #region class CollectionExtensions
    internal static class CollectionExtensions
    {
        #region Traverse
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Action<T> action) { return Traverse(source, null, action); }
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, Action<T> action) { return Traverse(source, fnRecurse, TraverseOrder.PreOrder, action); }
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order, Action<T> action) { return Traverse(source, fnRecurse, order, TraverseOptions.All, action); }

        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse) { return Traverse(source, fnRecurse, TraverseOrder.PreOrder); }
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order) { return Traverse(source, fnRecurse, TraverseOrder.PreOrder, TraverseOptions.All); }
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order, TraverseOptions options) { return Traverse(source, fnRecurse, order, options, null); }
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order, TraverseOptions options, Action<T> action)
        {
            if (source == null) { yield break; }
            foreach (T item in source)
            {
                if (order == TraverseOrder.PreOrder || order == TraverseOrder.InOrder) { if (action != null) { action(item); } yield return item; }
                var seqRecurse = fnRecurse != null ? fnRecurse(item) : null;
                if (seqRecurse != null)
                {
                    foreach (T itemRecurse in Traverse(seqRecurse, fnRecurse, order, options, action)) { if (action != null) { action(item); } yield return itemRecurse; }
                }
                if (order == TraverseOrder.PostOrder) { if (action != null) { action(item); } yield return item; }
            }
        }
        #endregion
        #region Iterate
        public static IEnumerable<T> Iterate<T>(this T source, Func<T, T> fnRecurse, TraverseOrder order = TraverseOrder.InOrder) { var coll = new[] { source }; Func<T, IEnumerable<T>> fnRecurse1 = t => new[] { fnRecurse(t) }; return coll.Iterate(fnRecurse1, order); }
        public static IEnumerable<T> Iterate<T>(this T source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order = TraverseOrder.InOrder) { var coll = new[] { source }; return coll.Iterate(fnRecurse, order); }
        public static IEnumerable<T> Iterate<T>(this IEnumerable<T> source, Func<T, T> fnRecurse, TraverseOrder order = TraverseOrder.InOrder) { Func<T, IEnumerable<T>> fnRecurse1 = t => new[] { fnRecurse(t) }; return source.Iterate(fnRecurse1, order); }
        public static IEnumerable<T> Iterate<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse, TraverseOrder order = TraverseOrder.InOrder)
        {
            if (source == null) { yield break; }
            foreach (T item in source)
            {
                if (order == TraverseOrder.PreOrder) { yield return item; }
                var seqRecurse = fnRecurse != null ? fnRecurse(item) : null;

                if (seqRecurse != null)
                {
                    if (seqRecurse.Count() == 2 && order == TraverseOrder.InOrder)
                    {
                        yield return seqRecurse.FirstOrDefault();
                        yield return item;
                        yield return seqRecurse.LastOrDefault();
                    }
                    else
                    {
                        if (order == TraverseOrder.InOrder) { yield return item; }
                        foreach (T itemRecurse in Iterate(seqRecurse, fnRecurse, order)) { yield return itemRecurse; }
                    }
                }

                if (order == TraverseOrder.PostOrder) { yield return item; }
            }
        }
        #endregion

        #region MapValue
        public static V MapValue<K, V>(this IDictionary<K, V> values, K key)
        {
            return values[key];
        }
        #endregion
        #region MapValue
        public static V MapValue<K, V>(this IDictionary<K, V> values, K key, V defaultValue)
        {
            var value = defaultValue;
            var ok = values.TryGetValue(key, out value);
            return ok ? value : defaultValue;
        }
        #endregion

        #region ForEach
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null || action == null) { return source; }
            foreach (var item in source) { action(item); }
            return source;
        }
        #endregion
        #region ToList
        public static R ToList<R>(this IEnumerable source) where R : IList, new()
        {
            if (source == null) { return default(R); }
            var retList = new R();
            foreach (var item in source) { retList.Add(item); }
            return retList;
        }
        #endregion

        #region FindIndex
        public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> fnFilter)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (fnFilter == null) throw new ArgumentNullException("fnFilter");

            int retVal = 0;
            foreach (var item in source)
            {
                if (fnFilter(item)) return retVal;
                retVal++;
            }
            return -1;
        }
        #endregion
        #region SelectItem
        public static T SelectItem<T>(this IList values, int i) { return values.SelectItem(i, default(T)); }
        public static T SelectItem<T>(this IList values, int i, T fallbackValue)
        {
            return values != null && values.Count > i && values[i] is T ? (T)values[i] : fallbackValue;
        }
        #endregion

        #region SingleItemAsEnumerable
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
        {
            yield return item;
        }
        #endregion
        #region IsNullOrEmpty
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
        #endregion
        #region IsEmpty
        public static bool IsEmpty(this IList source)
        {
            return source == null || source.Count == 0;
        }
        #endregion
        #region GetValue
        public static v GetValue<k, v>(this IDictionary<k, v> dic, k key) where v : new()
        {
            v ret = default(v);
            bool ok = dic.TryGetValue(key, out ret);
            if (!ok) { ret = new v(); dic[key] = ret; }
            return ret;
        }
        #endregion
        #region IList
        public static T LastOrDefault<T>(this IList<T> coll)
        {
            if (coll == null || coll.Count == 0)
                return default(T);
            return coll[coll.Count - 1];
        }
        public static T LastOrDefault<T>(this IList<T> coll, Func<T, bool> func)
        {
            if (coll == null || coll.Count == 0)
                return default(T);
            for (int i = coll.Count - 1; i >= 0; i--)
            {
                if (func(coll[i]))
                    return coll[i];
            }
            return default(T);
        }
        #endregion
    }
    #endregion
}
