using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace TSP
{
    public static class IEnumerableUtility
    {
        public static void WriteToFile(this IEnumerable<int> enumerable, string filename)
        {
            var writer = new StreamWriter(filename);

            foreach (int n in enumerable)
                writer.WriteLine(n);

            writer.Close();
        }

        public static void WriteToFileAsync(this IEnumerable<int> enumerable, string filename)
        {
            (new Thread(delegate() { WriteToFile(enumerable, filename); })).Start();
        }

        public static void Print(this IEnumerable<int> enumerable)
        {
            foreach (int n in enumerable)
                Console.Write(n + " ");
        }
    }

    public static class RandomUtility
    {
        public static int NextDifferent(this Random random, int inclusiveMin, int exclusiveMax, int tabu)
        {
            int value = random.Next(inclusiveMin, exclusiveMax);

            while (value == tabu)
                value = random.Next(inclusiveMin, exclusiveMax);

            return value;
        }
    }
}
