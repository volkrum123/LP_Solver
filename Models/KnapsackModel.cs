using System;
using System.Collections.Generic;

namespace LP_Solver.Models
{
    internal sealed class KnapsackItem
    {
        public int Index { get; }
        public int Weight { get; }
        public int Value { get; }
        public double Ratio => (double)Value / Weight; // weights are validated > 0 in parser

        public KnapsackItem(int index, int weight, int value)
        {
            if (weight <= 0) throw new ArgumentException("Weight must be > 0");
            if (value < 0) throw new ArgumentException("Value must be >= 0");
            Index = index;
            Weight = weight;
            Value = value;
        }
    }

    internal sealed class KnapsackModel
    {
        public bool IsMaximize { get; }
        public bool WasMinConvertedToMax { get; }
        public int Capacity { get; }
        public List<KnapsackItem> Items { get; }

        public KnapsackModel(bool isMaximize, bool wasMinConvertedToMax, int capacity, List<KnapsackItem> items)
        {
            IsMaximize = isMaximize;
            WasMinConvertedToMax = wasMinConvertedToMax;
            Capacity = capacity;
            Items = items;
        }
    }

    internal sealed class KnapsackResult
    {
        public int Capacity { get; internal set; }
        public int BestValue { get; internal set; }
        public int BestWeight { get; internal set; }
        public bool[] DecisionVector { get; internal set; } = Array.Empty<bool>();
        public List<KnapsackItem> ItemsTaken { get; internal set; } = new List<KnapsackItem>();
        public int NodesExplored { get; internal set; }
        public int NodesPruned { get; internal set; }
    }

    // Trace types (kept lean and together)
    internal sealed class KnapsackTrace
    {
        public List<(int originalIndex, int weight, int value, double ratio, int rank)> RatioTable
            = new List<(int, int, int, double, int)>();

        public List<TraceNode> Nodes = new List<TraceNode>();
    }
    internal sealed class TraceNode
    {
        public string Path = "";
        public int Level;
        public int? Decision;
        public int ItemSortedIndex;
        public int ItemOriginalIndex;
        public int Weight;
        public int Value;
        public double Bound;
        public string Status = "";
        public string Reason = "";
    }

}
