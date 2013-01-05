using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace TSP
{
    public class TaskLogger
    {
        private static int refreshInterval = 1000;
        private static bool started = false;
        private static bool progressHidden = false;
        private static double prevProgress = 0.0;

        public static int RefreshInterval
        {
            get { return refreshInterval; }
            set { refreshInterval = value; }
        }

        public static string TaskName { get; set; }
        public static string Text { get; set; }
        public static string Footer { get; set; }
        public static double Progress { get; set; }

        public static void HideProgress()
        {
            progressHidden = true;
        }

        public static void ShowProgress()
        {
            progressHidden = false;
        }

        public static void Start()
        {
            if (started)
                return;

            started = true;

            var thread = new Thread(delegate()
            {
                while (started)
                {
                    Refresh();
                    Thread.Sleep(refreshInterval);
                }
            });

            thread.Start();
        }

        public static void Stop()
        {
            started = false;
        }


        private static void Refresh()
        {
            Console.Clear();
            Console.WriteLine(TaskName);
            NewLine();

            PrintSpaces(4);
            Console.WriteLine(Text);

            if (!progressHidden)
            {
                NewLine();
                PrintSpaces(4);
                PrintProgressBar();
                PrintSpaces(2);
                Console.Write("{0:N2}%", Progress);
                PrintSpaces(4);
                PrintEstimatedTimeLeft();
            }

            NewLine();
            NewLine();
            NewLine();
            Console.WriteLine(Footer);
        }

        private static void PrintProgressBar()
        {
            int nprog = (int)(Progress * 0.2);

            Console.Write("|");

            for (int i = 0; i < 21; i++)
            {
                if (nprog >= i)
                    Console.Write("*");
                else
                    Console.Write(" ");
            }

            Console.Write("|");
        }

        private static void PrintEstimatedTimeLeft()
        {
            double currentProgress = Progress;
            double diff = currentProgress - prevProgress;

            if (diff <= 0)
                return;

            double interval = refreshInterval / 1000.0;
            double speed = diff / interval;
            double progressLeft = (100.0 - currentProgress);
            double timeLeft = progressLeft / speed;

            var timeSpan = TimeSpan.FromSeconds(timeLeft);

            Console.Write("Time left: ");

            if (timeSpan.Hours > 0)
                Console.Write("{0}h ", timeSpan.Hours);

            if (timeSpan.Minutes > 0)
                Console.Write("{0:00}m ", timeSpan.Minutes);

            Console.Write("{0:00}s", timeSpan.Seconds);

            prevProgress = currentProgress;
        }

        private static void PrintSpaces(int number)
        {
            for (int i = 0; i < number; i++)
                Console.Write(" ");
        }

        private static void NewLine()
        {
            Console.WriteLine();
        }
    }
}
