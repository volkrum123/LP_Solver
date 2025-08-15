using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Controllers
{
    internal class LPController
    {
        private LPParser _parser;
        private SimplexSolver _solver;

        public LPController()
        {
            _parser = new LPParser();
            _solver = new SimplexSolver();
        }

        public void SolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for(int i = 0; i < model.Constraints.Count; i++)
            {
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");
            }

            var tableau = _solver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            logOutput("\r\nInitial Tableau:\r\n" + _solver.TableauToString(tableau,numVariables,numConstraints));
            _solver.Solve(tableau, model.ObjectiveType,logOutput, numVariables, numConstraints);
        }
    }
}
