using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCompare
{
    internal static class ArrayComparer
    {
        internal static bool CopyArrayRank1<T>(T[] xArray, T[] yArray, CompareContext context)
        {
            if (ReferenceEquals(xArray, yArray))
                return true;

            var length = xArray.Length;
            if (length != yArray.Length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                var x = xArray[i];
                var y = yArray[i];

                if (context.Skip(x, y) == false)
                {
                    var result = ComparerGenerator<T>.Compare(x, y, context);
                    if (result == false)
                        return false;
                }
            }
            return true;
        }

        internal static bool CopyArrayRank2<T>(T[,] xArray, T[,] yArray, CompareContext context)
        {
            if (ReferenceEquals(xArray, yArray))
                return true;

            var lenI = xArray.GetLength(0);
            var lenJ = xArray.GetLength(1);

            if (lenI != yArray.GetLength(0) || lenJ != yArray.GetLength(1))
                return false;

            for (var i = 0; i < lenI; i++)
            {
                for (var j = 0; j < lenJ; j++)
                {
                    var x = xArray[i, j];
                    var y = xArray[i, j];
                    if (context.Skip(x, y) == false)
                    {
                        var result = ComparerGenerator<T>.Compare(x, y, context);
                        if (result == false)
                            return false;
                    }
                }
            }
            return true;
        }



        internal static bool CopyArray<T>(T x, T y, CompareContext context)
        {
            if (ReferenceEquals(x, y))
                return true;

            var xArray = x as Array;
            if (xArray == null) throw new InvalidCastException($"Cannot cast non-array type {x?.GetType()} to Array.");
            var yArray = y as Array;
            if (yArray == null) throw new InvalidCastException($"Cannot cast non-array type {y?.GetType()} to Array.");

            var rank = xArray.Rank;
            if (rank != yArray.Rank)
                return false;
            var lengths = new int[rank];
            for (var i = 0; i < rank; i++)
            {
                var length = xArray.GetLength(i);
                if (length != yArray.GetLength(i))
                    return false;
                lengths[i] = length;
            }

            var index = new int[rank];
            var sizes = new int[rank];
            sizes[rank - 1] = 1;

            for (var k = rank - 2; k >= 0; k--)
            {
                sizes[k] = sizes[k + 1] * lengths[k + 1];
            }
            for (var i = 0; i < xArray.Length; i++)
            {
                var k = i;
                for (var n = 0; n < rank; n++)
                {
                    var offset = k / sizes[n];
                    k = k - offset * sizes[n];
                    index[n] = offset;
                }

                var xItem = xArray.GetValue(index);
                var yItem = yArray.GetValue(index);

                if (context.Skip(xItem, yItem) == false)
                {
                    var result = DeepComparer.Compare(xItem, yItem, context);
                    if (result == false)
                        return false;
                }
            }
            return true;
        }
    }
}
