using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TSP
{
    public class GeneticTspEngine
    {
        public class Solution : IntegerPermutation
        {
            private PointArray nodePositions;
            private double distance = 0;

            public double Distance
            {
                get { return (distance == 0) ? (distance = nodePositions.GetDistance(this)) : (distance); }
            }

            internal Solution(PointArray nodesPosition, IntegerPermutation permutation) : base(permutation) 
            {
                this.nodePositions = nodesPosition;
            }
        }

        private Random random;

        private PointArray nodePositions;
        private List<Solution> population;

        public int PopulationSize { get; set; }
        public double MutationProbability { get; set; }
        public double EliteFactor { get; set; }
        public double NNProbability { get; set; }

        public int CurrentGeneration { get; private set; }
        public Solution CurrentBestSolution { get; private set; }
        public IEnumerable<Solution> CurrentSolutionPool
        {
            get { return population; }
        }

        public GeneticTspEngine(PointArray nodePositions)
        {
            this.nodePositions = nodePositions;
            this.population = new List<Solution>();

            this.PopulationSize = 100;
            this.MutationProbability = 0.02;
            this.EliteFactor = 0.05;
            this.NNProbability = 0.02;

            this.CurrentGeneration = 0;

            this.random = new Random();
        }

        private Solution CreateSolution(IntegerPermutation permutation)
        {
            return new Solution(nodePositions, permutation);
        }

        private Solution GenerateRandomSolution()
        {
            double discriminator = random.NextDouble();
            IntegerPermutation result;

            if (discriminator > this.NNProbability)
            {
                var availableNumbers = IntegerRandomAccessSet.CreateFullSet(nodePositions.Length);
                int index = 0;

                result = new IntegerPermutation(nodePositions.Length);
                while (availableNumbers.ItemsCount > 0)
                {
                    int randomValue = random.Next(0, availableNumbers.ItemsCount);
                    result[index++] = availableNumbers[randomValue];
                    availableNumbers.RemoveAt(randomValue);
                }
            }
            else
            {
                int random1 = random.Next(0, nodePositions.Length);
                int random2 = random.NextDifferent(0, nodePositions.Length, random1);

                var startEdge = new Edge(random1, random2);
                var nnTourFinder = new NNTourFinder(nodePositions);

                result = new IntegerPermutation(nnTourFinder.FindTourStartingFrom(startEdge));
            }

            return this.CreateSolution(result);
        }

        private IEnumerable<Solution> Crossover(IntegerPermutation parent1, IntegerPermutation parent2)
        {
            // Uses 0x3 heuristic

            var offspring1 = new IntegerPermutation(parent1.Length);
            var offspring2 = new IntegerPermutation(parent2.Length);

            int subsequenceLength = random.Next(3, Math.Max(3, parent1.Length / 3));

            int start1 = random.Next(0, parent1.Length - subsequenceLength);
            int end1 = start1 + subsequenceLength;
            int start2 = random.Next(0, parent2.Length - subsequenceLength);
            int end2 = start2 + subsequenceLength;

            // Selected segments are copied
            var copiedSubsequence1 = parent1[start1, end1].ToArray();
            var copiedSubsequence2 = parent2[start2, end2].ToArray();
            offspring1[start1, end1] = copiedSubsequence1;
            offspring2[start2, end2] = copiedSubsequence2;

            // Sort elements in array subsequence for binary search
            Array.Sort(copiedSubsequence1);
            Array.Sort(copiedSubsequence2);

            // Copies remaining cities of parent2 in offspring1
            int parentIndex = parent2.GetNextIndex(end2);
            int offspringIndex = offspring1.GetNextIndex(end1);
            
            while (offspringIndex != start1)
            {
                int current = parent2[parentIndex];

                // If this item already belongs to the offspring, skips it
                if (Array.BinarySearch(copiedSubsequence1, current) >= 0)
                {
                    parentIndex = parent2.GetNextIndex(parentIndex);
                    continue;
                }

                offspring1[offspringIndex] = current;
                offspringIndex = offspring1.GetNextIndex(offspringIndex);
                parentIndex = parent2.GetNextIndex(parentIndex);
            }

            // Copies remaining cities of parent1 in offspring2
            parentIndex = parent1.GetNextIndex(end1);
            offspringIndex = offspring2.GetNextIndex(end2);

            while (offspringIndex != start2)
            {
                int current = parent1[parentIndex];

                // If this item already belongs to the offspring, skips it
                if (Array.BinarySearch(copiedSubsequence2, current) >= 0)
                {
                    parentIndex = parent1.GetNextIndex(parentIndex);
                    continue;
                }

                offspring2[offspringIndex] = current;
                offspringIndex = offspring2.GetNextIndex(offspringIndex);
                parentIndex = parent1.GetNextIndex(parentIndex);
            }

            yield return this.CreateSolution(offspring1);
            yield return this.CreateSolution(offspring2);
        }

        private IEnumerable<Solution> PickRandomParents()
        {
            int random1 = random.Next(0, population.Count);
            int random2 = random.NextDifferent(0, population.Count, random1);

            yield return population[random1];
            yield return population[random2];
        }

        private void Mutate(IntegerPermutation solution)
        {
            int magnitude = (int)(solution.Length * 0.01 * (0.5 + random.NextDouble()));

            for (int i = 0; i < magnitude; i++)
            {
                int random1 = random.Next(0, solution.Length);
                int random2 = random.NextDifferent(0, solution.Length, random1);

                var temp = solution[random1];
                solution[random1] = solution[random2];
                solution[random2] = temp;
            }
        }

        private void Initialize()
        {
            TaskLogger.Text = "Generating random solution pool...";

            while (population.Count < this.PopulationSize)
            {
                population.Add(this.GenerateRandomSolution());
                TaskLogger.Progress = 100.0 * population.Count / this.PopulationSize;
            }
        }

        public void StartFromKnownSolutions(params IntegerPermutation[] baseSolutions)
        {
            population.Clear();
            foreach (var baseSolution in baseSolutions)
                population.Add(this.CreateSolution(baseSolution));
        }

        public void NextGeneration()
        {
            if (this.CurrentGeneration == 0)
                this.Initialize();

            TaskLogger.Text = string.Format("Generation {0} - Exterminating existing non-elite population...", this.CurrentGeneration);
            TaskLogger.HideProgress();

            // Numbers of elite elements
            int eliteSize = (int)(this.EliteFactor * population.Count);

            // Sort solutions by distance in ascending order
            population.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));

            this.CurrentBestSolution = population[0];

            // Exterminate all the population but the elite
            population.RemoveRange(eliteSize, population.Count - eliteSize);

            TaskLogger.Text = string.Format("Generation {0} - Breeding and mutating population...", this.CurrentGeneration);

            // Refill the population pool
            while (population.Count < this.PopulationSize)
            {
                // Perform crossover between two random elite parents
                var parents = this.PickRandomParents().ToArray();
                var children = this.Crossover(parents[0], parents[1]).ToArray();

                population.AddRange(children);

                if (population.Count >= this.PopulationSize)
                    break;

                // And eventually mutate a solution
                if (random.NextDouble() <= this.MutationProbability)
                {
                    if (random.NextDouble() < 0.5)
                        this.Mutate(children[0]);
                    else
                        this.Mutate(children[1]);
                }
            }

            this.CurrentGeneration++;
        }

        public void Reset()
        {
            this.CurrentGeneration = 0;
            this.CurrentBestSolution = null;
        }
    }
}
