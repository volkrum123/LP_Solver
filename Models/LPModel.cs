using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    // Creates a enum menu of the possible sign restrictions
    internal enum VariableType
    {
        Continuous,
        Integer,
        Binary
    }
    internal class VariableInfo
    {
        public int Index { get; set; }  // gets the variable name           
        public double LowerBound { get; set; } = 0; // gets the lowerbound value of the variables or defaults sets it to 0.
        public double UpperBound { get; set; } = double.PositiveInfinity; // gets the lowerbound value of the variables
        public VariableType Type { get; set; } = VariableType.Continuous;
    }

    internal class LPModel
    {
        public string ObjectiveType { get; set; }  // gets the objective type (Max or Min)
        public List<double> ObjectiveCoefficients { get; set; } // gets the objective function variable values (4x1 + 5x2 + 6x3)
        public List<string> Constraints { get; set; } // Gets constraints <=, =>, =
        public List<int> IntegerIndices { get; set; } = new List<int>(); //Checks for Integer sign restrictions.
        public List<VariableInfo> Variables { get; set; } = new List<VariableInfo>(); // checks for binary and continues sign restrictions
        public int NumVariables => ObjectiveCoefficients?.Count ?? 0;
        public LPModel()  // creates a model constructer which will be used by the simplexes.
        {
            ObjectiveCoefficients = new List<double>();
            Constraints = new List<string>();
        }
    }
}
