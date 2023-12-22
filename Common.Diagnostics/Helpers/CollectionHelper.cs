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
    internal static class CollectionHelper
    {
        #region TryGetItem
        public static T TryGetItem<T>(object[] values, int i)
        {
            T ret = values != null && values.Length > i && values[i] is T ? (T)values[i] : default(T);
            return ret;
        }
        #endregion

        #region Clone
        public static R Clone<R>(IEnumerable list) where R : IList, new()
        {
            R newList = new R();
            if (list != null)
            {
                foreach (var item in list)
                {
                    newList.Add(item);
                }
            }
            return newList;
        }
        #endregion
        #region Clone
        public static R Clone<R>(IEnumerable list, bool preserveNull) where R : IList, new()
        {
            R retList = default(R);
            if (list != null)
            {
                retList = new R();
                foreach (var item in list)
                {
                    retList.Add(item);
                }
            }
            return retList;
        }
        #endregion
        #region ReplaceItem
        public static bool ReplaceItem<T>(IList<T> list, T item, T replace) // where T : class
        {
            if (list == null) { return false; }
            var index = list.IndexOf(item);
            if (index >= 0)
            {
                list.RemoveAt(index);
                list.Insert(index, replace);
                return true;
            }
            return false;
        }
        #endregion
    }
}
