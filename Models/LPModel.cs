using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class LPModel
    {
        public string ObjectiveType { get; set; }
        public List<int> ObjectiveCoefficients { get; set; }
        public List<string> Constraints { get; set; }

        public LPModel()
        {
            ObjectiveCoefficients = new List<int>();
            Constraints = new List<string>();
        }
    }
           
}
