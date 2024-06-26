using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
	static class Algorithm
	{
        /// <summary>
        /// Bubble sort algorithm implementation
        /// Sorts list until comparer(list[i], list[i+1]) is true for all i
        /// </summary>
        public static void BubbleSort<T>(IList<T> list, Func<T, T, bool> comparer)
        {
            int count = list.Count;
            for (int i = 0; i < count - 1; i++) {
                bool swapped = false;
                for (int j = 0; j < count - i - 1; j++) {
                    if (comparer(list[j], list[j + 1]) == false)
                    {
                        T temp = list[j];
                        list[j] = list[j + 1];
                        list[j + 1] = temp;
                        swapped = true;
                    }
                }

                if (swapped == false) break;
            }
        }

        public static float CalculateStandardDeviation(IEnumerable<float> values, float averageValue)
        {
            return (float)Math.Sqrt(values.Average(v => (float)Math.Pow(v - averageValue, 2)));
        }
    }
}
