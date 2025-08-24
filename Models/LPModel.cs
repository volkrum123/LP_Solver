using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal enum VariableType
    {
        Continuous,
        Integer,
        Binary
    }
    internal class VariableInfo
    {
        public int Index { get; set; }              // x1 -> 0, x2 -> 1, etc.
        public double LowerBound { get; set; } = 0; // default: x >= 0
        public double UpperBound { get; set; } = double.PositiveInfinity;
        public VariableType Type { get; set; } = VariableType.Continuous;
    }

    internal class LPModel
    {
        public string ObjectiveType { get; set; }
        public List<double> ObjectiveCoefficients { get; set; }
        public List<string> Constraints { get; set; }

        // Old style integer indices (still works for backwards compatibility)
        public List<int> IntegerIndices { get; set; } = new List<int>();

        // ✅ New: Per-variable metadata (bounds, type, etc.)
        public List<VariableInfo> Variables { get; set; } = new List<VariableInfo>();

        public int NumVariables => ObjectiveCoefficients?.Count ?? 0;

        public LPModel()
        {
            ObjectiveCoefficients = new List<double>();
            Constraints = new List<string>();
        }
    }

    /*
    internal class LPModel
    {
        public string ObjectiveType { get; set; }
        public List<double> ObjectiveCoefficients { get; set; }
        public List<string> Constraints { get; set; }
        public List<int> IntegerIndices { get; set; } = new List<int>();
        public int NumVariables => ObjectiveCoefficients?.Count ?? 0;


        public LPModel()
        {
            ObjectiveCoefficients = new List<double>();
            Constraints = new List<string>();
        }
    }
     */
}
