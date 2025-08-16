using System.Collections.Generic;

namespace LP_Solver.Models
{
    internal class KnapsackResult
    {
        public int Capacity { get; internal set; }
        public int BestValue { get; internal set; }
        public int BestWeight { get; internal set; }
        public bool[] DecisionVector { get; internal set; } = new bool[0];
        public List<KnapsackItem> ItemsTaken { get; internal set; } = new List<KnapsackItem>();
        public int NodesExplored { get; internal set; }
        public int NodesPruned { get; internal set; }
    }
}