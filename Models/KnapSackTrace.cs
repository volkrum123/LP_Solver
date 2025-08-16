using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    // Overall trace container
    internal class KnapsackTrace
    {
        public List<(int originalIndex, int weight, int value, double ratio, int rank)> RatioTable
            = new List<(int, int, int, double, int)>();

        public List<TraceNode> Nodes = new List<TraceNode>();
    }

    // One log line per node action
    internal class TraceNode
    {
        public string Path;           // e.g. "P", "P.1", "P.1.0"
        public int Level;             // next item idx in sorted order (0..n)
        public int? Decision;         // null=root, 1=include, 0=exclude
        public int ItemSortedIndex;   // which item considered at this branch (sorted)
        public int ItemOriginalIndex; // original index in the input list
        public int Weight;
        public int Value;
        public double Bound;
        public string Status;         // "Expand", "Push", "Prune", "Incumbent"
        public string Reason;         // e.g. "infeasible", "bound<=incumbent"
    }
}
