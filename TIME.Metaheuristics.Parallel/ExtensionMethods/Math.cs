using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TIME.Metaheuristics.Parallel.ExtensionMethods
{
    static public class Math
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (min.CompareTo(max) > 0)
            {
                T temp = min;
                min = max;
                max = temp;
            }

            if (val.CompareTo(min) < 0)
                return min;
            return val.CompareTo(max) > 0 ? max : val;
        }
    }
}
