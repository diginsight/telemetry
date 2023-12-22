using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    internal static class ArrayHelper
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static byte[] SwapBytesInWord(byte[] ba)
        {
            for (int i = 0; i < ba.Length - 1; i += 2)
            {
                byte temp = ba[i];
                ba[i] = ba[i + 1];
                ba[i + 1] = temp;
            }

            return ba;
        }

        public static string ByteArrayToString(byte[] array, string itemFormat = "{0:X2}", string separator = "")
        {
            if (array == null) { return null; }
            var res = string.Join(separator, array.Select((item) => string.Format(itemFormat, item)).ToArray());
            return res;
        }
        public static string ShortArrayToString(short[] array, string itemFormat = "{0:X4}", string separator = " ")
        {
            if (array == null) { return null; }
            var res = string.Join(separator, array.Select((item) => string.Format(itemFormat, item)).ToArray());
            return res;
        }
        public static string IntArrayToString(int[] array, string itemFormat = "{0:X8}", string separator = " ")
        {
            if (array == null) { return null; }
            var res = string.Join(separator, array.Select((item) => string.Format(itemFormat, item)).ToArray());
            return res;
        }
    }
}
