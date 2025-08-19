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
        private DuelSimplexSolver _dualSolver;
        private BranchAndBoundSolver _bbSolver;
        private CanonicalForm _canonicalForm;


        public LPController()
        {
            _parser = new LPParser();
            _solver = new SimplexSolver();
            _dualSolver = new DuelSimplexSolver();
            _bbSolver = new BranchAndBoundSolver();
            _canonicalForm = new CanonicalForm();
        }

        public void SolveFromInput(string input, Action<string> logOutput)
        {
            // Basic model
            var model = _parser.Parse(input);
            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for(int i = 0; i < model.Constraints.Count; i++)
            {
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");
            }

            //Canonical From
            string canonicalForm = _canonicalForm.ConvertToCanonicalFormSequential(model);// call your method here
            logOutput("\r\n" + canonicalForm + "\r\n");

            //Initial Tablue
            
            var (tableau, ConstraintTypes) = _solver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            logOutput("\r\nInitial Tableau:\r\n" +
                 _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

           double[,] OptimalTable = _solver.Solve(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);

        }

        public void DualSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);

            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for (int i = 0; i < model.Constraints.Count; i++)
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");

            // Create tableau
            var (tableau, ConstraintTypes) = _dualSolver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            // Print initial tableau
            logOutput("\r\nInitial Tableau:\r\n" +
                _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

            // Solve using dual simplex
            double[,] OptimalTable = _dualSolver.SolveDual(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);

        }

        public void BranchAndBoundSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            if (model.IntegerIndices == null || model.IntegerIndices.Count == 0)
            {
                // default all decision variables integer (common in assignments)
                for (int i = 0; i < model.ObjectiveCoefficients.Count; i++) model.IntegerIndices.Add(i);
            }
            _bbSolver.SolveBranchAndBound(model, logOutput);
        }
    }
}
