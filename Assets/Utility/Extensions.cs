using System.Collections.Generic;
using UnityEngine;

namespace Assets.Utility
{
    public static class Extensions
    {
        public static Vector2Int Dimensions(this float[,] matrix) => new Vector2Int(matrix.GetLength(0), matrix.GetLength(1));

        public static IEnumerable<int> Factors(this int x)
        {
            if (x < 0) yield break;
            if (x == 1) yield return 1;
            else
            {
                for (int i = 1; i * i <= x; i++)
                {
                    if (0 != (x % i)) continue;
                    yield return i;
                    if (i != x / i) yield return x / i;
                }
            }
        }
    }
}