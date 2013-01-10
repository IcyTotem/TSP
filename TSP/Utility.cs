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

    public static class ThreadStarter
    {
        public static Thread Start<T>(Action<T> method, T param)
        {
            Thread thread = new Thread(delegate()
            {
                method(param);
            });

            thread.Start();

            return thread;
        }

        public static Thread Start<T1, T2>(Action<T1, T2> method, T1 param1, T2 param2)
        {
            Thread thread = new Thread(delegate()
            {
                method(param1, param2);
            });

            thread.Start();

            return thread;
        }

        public static void JoinAll(this IEnumerable<Thread> threadCollection)
        {
            foreach (var thread in threadCollection)
                thread.Join();
        }
    }
}
