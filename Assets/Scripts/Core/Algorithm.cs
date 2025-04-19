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

        /// <summary>
        /// Converts a string like "1.12.4" to an array of integers like [1, 12, 4]. If the formatting is wrong,
        /// or there is not exactly three numbers in a version string, returns null.
        /// </summary>
        public static int[] ParseVersion(string version) => ParseVersion(version, 3);

        public static int[] ParseVersion(string version, int depth) => ParseVersion(version, depth, depth);

        public static int[] ParseVersion(string version, int minDepth, int maxDepth, bool allowLeadingZeros = false)
        {
            if (version is null) return null;
            string[] numbers = version.Split(new char[] { '.' }, StringSplitOptions.None);
            if (numbers.Length < minDepth || numbers.Length > maxDepth) return null;
            int[] ints = new int[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                if (!int.TryParse(numbers[i], out ints[i])) return null;
                if (!allowLeadingZeros && numbers[i][0] == '0' && numbers[i].Length > 1) return null;
                if (ints[i] < 0) return null;
            }

            return ints;
        }

        /// <summary>
        /// -1 if a < b, 0 if a == b, and 1 if a > b. Both version arrays should be the same length
        /// </summary>
        public static int CompareVersions(int[] a, int[] b) {
            if (a is null || b is null) throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
            if (a.Length != b.Length) throw new ArgumentException("Versions should be the same length");

            for (int i = 0; i < a.Length; i++) {
                if (a[i] < b[i]) return -1;
                if (a[i] > b[i]) return 1;
            }

            return 0;
        }

        public static int CompareVersions(string a, string b) {
            return CompareVersions(ParseVersion(a, 0, a.Length), ParseVersion(b, 0, b.Length));
        }
    }
}
