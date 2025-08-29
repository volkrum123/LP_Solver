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
        private RevisedPrimal _revised;
        private CuttingPlaneSolver _cuttingPlaneSolver;
        private SensitivityAnalysis _sensitivity;
        private double[,] LastOptimalTableau;
        private List<int> LastBasicIndices;



        public LPController()
        {
            _parser = new LPParser();
            _solver = new SimplexSolver();
            _dualSolver = new DuelSimplexSolver();
            _bbSolver = new BranchAndBoundSolver();
            _canonicalForm = new CanonicalForm();
            _revised = new RevisedPrimal();
        }

        public void SolveFromInput(string input, Action<string> logOutput)
        {
            // Basic model
            var model = _parser.Parse(input);
            LogModelAndStandardform(model, logOutput );
            //Initial Tablue
            
            var (tableau, ConstraintTypes) = _solver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            logOutput("\r\nInitial Tableau:\r\n" +
                 _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

           double[,] OptimalTable = _solver.Solve(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);
            LastOptimalTableau = OptimalTable;
            LastBasicIndices = _solver.GetBasicIndices();
            // Initialize sensitivity
            _sensitivity = new SensitivityAnalysis(model, LastOptimalTableau, LastBasicIndices);

        }

        public void RevisedSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            double[] solution = _revised.Solve(model, logOutput);
            
        }

        public void DualSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            LogModelAndStandardform(model, logOutput);
           
            // Create tableau
            var (tableau, ConstraintTypes) = _dualSolver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            // Print initial tableau
            logOutput("\r\nInitial Tableau:\r\n" +
                _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

            // Solve using dual simplex
            double[,] OptimalTable = _dualSolver.SolveDual(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);

            LastOptimalTableau = OptimalTable;
            LastBasicIndices = _dualSolver.GetBasicIndices();

            _sensitivity = new SensitivityAnalysis(model, LastOptimalTableau, LastBasicIndices);

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
        public void CuttingPlaneSolveFromInput(string input, Action<string> logOutput)  // Keep as internal
        {
            var model = _parser.Parse(input);

            // Ensure IntegerIndices is not null
            if (model.IntegerIndices == null)
            {
                model.IntegerIndices = new List<int>();
            }

            // If no integer variables specified, use all variables as integer
            if (model.IntegerIndices.Count == 0)
            {
                logOutput("No integer variables specified. Using all variables as integer.\n");
                for (int i = 0; i < model.ObjectiveCoefficients.Count; i++)
                    model.IntegerIndices.Add(i);
            }

            var result = _cuttingPlaneSolver.Solve(model, logOutput);

            // You can access the result object for further processing if needed
            if (result.SolutionFound)
            {
                logOutput($"Optimal objective value: {result.ObjectiveValue}\n");
            }
        }
        public void LogModelAndStandardform(LPModel model, Action<string> logOutput)
        {
            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");
            }
            // Variables (bounds, type, integer/binary)
            for (int i = 0; i < model.Variables.Count; i++)
            {
                var v = model.Variables[i];

                // Determine type string
                string typeStr = v.Type switch
                {
                    VariableType.Continuous => "Continuous",
                    VariableType.Integer => "Integer",
                    VariableType.Binary => "Binary",
                    _ => "Unknown"
                };

                // Determine bounds string
                string bounds;
                if (double.IsPositiveInfinity(v.UpperBound))
                    bounds = $"{v.LowerBound} ≤ x{i + 1}";
                else
                    bounds = $"{v.LowerBound} ≤ x{i + 1} ≤ {v.UpperBound}";

                logOutput($"Variable x{i + 1}: Type={typeStr}, Bounds: {bounds}\r\n");
            }
            //Canonical From
            string canonicalForm = _canonicalForm.ConvertToCanonicalFormSequential(model);// call your method here
            logOutput("\r\n" + canonicalForm + "\r\n");
        }

        public string SensitivityAnalysisFromInput(string operation, string userInput)
        {
            if (_sensitivity == null)
                return "No solution available. Solve a model first.";

            switch (operation)
            {
                case "Display Reduced Costs":
                    return _sensitivity.DisplayReducedCosts();

                case "Display Shadow Prices":
                    return _sensitivity.DisplayShadowPrices();

                case "Display Objective Ranges":
                    return _sensitivity.DisplayObjectiveRanges();

                case "Display RHS Ranges":
                    return _sensitivity.DisplayRHSRanges();

                case "Apply Objective Coefficient Change":
                    if (ParseIndexValue(userInput, out int idxObj, out double valObj))
                        return _sensitivity.ApplyNonBasicVariableChange(idxObj, valObj);
                    return "Invalid input. Use format: index,value (e.g., 0,55)";

                case "Apply RHS Change":
                    if (ParseIndexValue(userInput, out int idxRHS, out double valRHS))
                        return _sensitivity.ApplyRHSChange(idxRHS, valRHS);
                    return "Invalid input. Use format: index,value (e.g., 1,25)";

                case "Apply Variable Change":
                    if (ParseIndexValue(userInput, out int idxVar, out double valVar))
                        return _sensitivity.ApplyVariableChange(idxVar, valVar);
                    return "Invalid input. Use format: index,value (e.g., 0,10)";

                default:
                    return "Invalid operation selected.";
               }

            }
        private bool ParseIndexValue(string input, out int index, out double value)
        {
            index = -1;
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string[] parts = input.Split(',');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out index)) return false;
            if (!double.TryParse(parts[1], out value)) return false;
            return true;
        }
    }
}
