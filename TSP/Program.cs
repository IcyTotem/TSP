using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace TSP
{
    class Program
    {
        const int InitializationThreadCount = 15;

        const double TargetDistance = 7000000;
        static readonly long sum150k = Enumerable.Range(0, 150000).Select(i => (long)i).Sum();

        const string InputFileName = "santa-cities.csv";
        const string BestTourFileName = "best-first-tour.dat";
        const string SecondBestTourFileName = "best-second-tour.dat";
        const string OneEdgeTourFileName = "one-edge-tour.dat";
        const string NNClusterFileName = "nncluster.dat";

        static string DataFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

        static PointArray cities;
        static NNClusterSet nnCluster;
        static IntegerPermutation bestTour, secondBestTour;
        static double bestTourDistance, secondBestTourDistance;

        static void Main(string[] args)
        {
            TaskLogger.Start();

            ParseData();

            InitializeClusterSet();
            InitializeBestTour();
            InitializeSecondBestTour();

            UpdateLoggerFooter();

            CrossOptimize();

            Console.WriteLine("Genetic?");
            Console.ReadKey();

            var optimizer = new TwoOptimizer(cities);
            var geneticEngine = new GeneticTspEngine(cities);
            var nnTourFinder = new NNTourFinder(cities);

            while (secondBestTourDistance > TargetDistance)
            {
                TaskLogger.TaskName = "Genetic algorithm on first tour";

                geneticEngine.StartFromKnownSolutions(bestTour);
                while (geneticEngine.CurrentGeneration < 1000)
                {
                    geneticEngine.NextGeneration();

                    var currentSolution = geneticEngine.CurrentBestSolution;
                    if (currentSolution.Distance < bestTourDistance)
                    {
                        bestTour = currentSolution;
                        bestTourDistance = currentSolution.Distance;
                        SaveTour(bestTour, BestTourFileName);
                        UpdateLoggerFooter();
                    }
                }

                TaskLogger.TaskName = "Optimizationg of first tour";
                TaskLogger.ShowProgress();
                var gain = 0.0;
                do
                {
                    var oldDistance = bestTourDistance;

                    optimizer.Optimize(bestTour);
                    bestTourDistance = cities.GetDistance(bestTour);
                    UpdateLoggerFooter();

                    gain = oldDistance - bestTourDistance;

                    if (gain > 0)
                        SaveTour(bestTour, BestTourFileName);
                } while (gain > 1000.0);


                TaskLogger.TaskName = "Genetic algorithm on second disjoint tour";
                geneticEngine.Reset();
                geneticEngine.StartFromKnownSolutions(
                    secondBestTour, 
                    new IntegerPermutation(nnTourFinder.FindRandomDisjointTour(bestTour)));

                while (geneticEngine.CurrentGeneration < 1000)
                {
                    geneticEngine.NextGeneration();

                    foreach (var currentSolution in geneticEngine.CurrentSolutionPool)
                    {
                        if (currentSolution.Distance > secondBestTourDistance)
                            break;

                        if (!CheckDisjointness(bestTour, secondBestTour))
                            continue;

                        if (currentSolution.Distance < secondBestTourDistance)
                        {
                            secondBestTour = currentSolution;
                            secondBestTourDistance = currentSolution.Distance;
                            SaveTour(secondBestTour, SecondBestTourFileName);
                            UpdateLoggerFooter();
                        }
                    }
                }

                TaskLogger.TaskName = "Optimization of second disjoint tour";
                TaskLogger.ShowProgress();
                do
                {
                    var oldDistance = secondBestTourDistance;

                    optimizer.OptimizeDisjoint(secondBestTour, bestTour);
                    secondBestTourDistance = cities.GetDistance(secondBestTour);
                    UpdateLoggerFooter();

                    gain = oldDistance - secondBestTourDistance;

                    if (gain > 0)
                        SaveTour(secondBestTour, SecondBestTourFileName);
                } while (gain > 1000.0);
            }

            TaskLogger.Stop();
        }


        static void ParseData()
        {
            TaskLogger.TaskName = "Parsing";
            TaskLogger.Text = "Reading and parsing input file...";
            TaskLogger.HideProgress();

            var reader = new StreamReader(InputFileName);
            var count = int.Parse(reader.ReadLine());

            cities = new PointArray(count);
            for (int i = 0; i < count; i++)
            {
                var coords = reader.ReadLine().Split(',');

                var x = int.Parse(coords[1]);
                var y = int.Parse(coords[2]);

                cities[i] = new Point() { X = x, Y = y };
            }

            TaskLogger.ShowProgress();
        }

        static IntegerPermutation LoadTour(string filename)
        {
            return new IntegerPermutation(new IntegerStream(Path.Combine(DataFolder, filename)));
        }

        static void SaveTour(IEnumerable<int> tour, string filename)
        {
            // Better to be safe: first saves data to a temporary file, then discard the old file (if any)
            var path = Path.Combine(DataFolder, filename);
            var tempPath = path + ".tmp";

            tour.WriteToFile(tempPath);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }

        static bool TourExists(string filename)
        {
            return File.Exists(Path.Combine(DataFolder, filename));
        }


        static Edge LoadOneEdgeTour()
        {
            var reader = new StreamReader(Path.Combine(DataFolder, OneEdgeTourFileName));
            int n1 = int.Parse(reader.ReadLine());
            int n2 = int.Parse(reader.ReadLine());

            return new Edge(n1, n2);
        }

        static void SaveOneEdgeTour(Edge edge)
        {
            var writer = new StreamWriter(Path.Combine(DataFolder, OneEdgeTourFileName));
            writer.WriteLine(edge.Head);
            writer.WriteLine(edge.Tail);
            writer.Close();

        }


        static void InitializeBestTour()
        {
            TaskLogger.TaskName = "Initializing first tour";
            if (TourExists(BestTourFileName))
            {
                bestTour = LoadTour(BestTourFileName);
            }
            else
            {
                Edge shortestEdge;

                if (TourExists(OneEdgeTourFileName))
                {
                    shortestEdge = LoadOneEdgeTour();
                }
                else
                {
                    var shortestEdgeFinder = new ShortestEdgeFinder(InitializationThreadCount);
                    shortestEdge = shortestEdgeFinder.FindInCompleteGraph(cities);
                    SaveOneEdgeTour(shortestEdge);
                }

                var nnTourFinder = new NNTourFinder(cities);
                bestTour = new IntegerPermutation(nnTourFinder.FindTourStartingFrom(shortestEdge));
                SaveTour(bestTour, BestTourFileName);
            }

            bestTourDistance = cities.GetDistance(bestTour);
        }

        static void InitializeSecondBestTour()
        {
            TaskLogger.TaskName = "Initializing second disjoint tour";
            if (TourExists(SecondBestTourFileName))
            {
                secondBestTour = LoadTour(SecondBestTourFileName);
            }
            else
            {
                var nnTourFinder = new NNTourFinder(cities);
                secondBestTour = new IntegerPermutation(nnTourFinder.FindRandomDisjointTour(bestTour));
                SaveTour(secondBestTour, SecondBestTourFileName);
            }

            secondBestTourDistance = cities.GetDistance(secondBestTour);
        }

        static void InitializeClusterSet()
        {
            var clusterPath = Path.Combine(DataFolder, NNClusterFileName);
            if (File.Exists(clusterPath))
            {
                nnCluster = NNClusterSet.DeserializeFrom(new FileStream(clusterPath, FileMode.Open), cities, 5);
            }
            else
            {
                TaskLogger.TaskName = "Clustering for future use";

                nnCluster = NNClusterSet.Build(cities, 5);
                nnCluster.SerializeOn(new FileStream(clusterPath, FileMode.Create));
            }
        }

        static void UpdateLoggerFooter()
        {
            TaskLogger.Footer = string.Format("Best tour distance: {0:N0}\nSecond tour distance: {1:N0}", bestTourDistance, secondBestTourDistance);
        }


        static void CrossOptimize()
        {
            var optimizer = new TwoOptimizer(cities);

            TaskLogger.TaskName = "Cross-optimization";

            optimizer.ClusterSet = nnCluster;
            optimizer.CrossOptimize(secondBestTour, bestTour);

            var newDistance = cities.GetDistance(secondBestTour);

            if (!AreTourValid())
            {
                TaskLogger.Stop();
                Console.WriteLine("Some error in the algorithm!!");
                Console.ReadKey();
                return;
            }

            if (newDistance < secondBestTourDistance && newDistance > bestTourDistance)
            {
                SaveTour(secondBestTour, SecondBestTourFileName);
                SaveTour(bestTour, BestTourFileName);
            }

            UpdateLoggerFooter();
        }


        public static bool AreTourValid()
        {
            long sum1 = bestTour.Select(i => (long)i).Sum();

            if (sum1 != sum150k)
                return false;

            long sum2 = secondBestTour.Select(i => (long)i).Sum();

            if (sum2 != sum150k)
                return false;

            bool disjoint = CheckDisjointness(bestTour, secondBestTour);

            return disjoint;
        }

        public static bool CheckDisjointness(IntegerPermutation tour1, IntegerPermutation tour2)
        {
            var tabuList = TabuEdgeCollection.CreateFromTour(tour1);

            for (int i = 1; i < tour2.Length; i++)
            {
                int head = tour2[i - 1];
                int tail = tour2[i];

                if (tabuList.IsTabu(head, tail))
                    return false;
            }

            return true;
        }
    }
        
}
