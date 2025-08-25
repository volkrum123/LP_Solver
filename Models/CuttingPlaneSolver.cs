using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class CuttingPlaneSolver
    {
        private const double Tolerance = 1e-6;
        private readonly SimplexSolver _simplexSolver;
        private readonly DuelSimplexSolver _dualSimplexSolver;
        private readonly CanonicalForm _canonicalForm;

        public CuttingPlaneSolver()
        {
            _simplexSolver = new SimplexSolver();
            _dualSimplexSolver = new DuelSimplexSolver();
            _canonicalForm = new CanonicalForm();
        }

        internal CuttingPlaneResult Solve(LPModel model, Action<string> logOutput)  // Changed to internal
        {
            // Handle null logOutput by creating a default action
            Action<string> safeLogOutput = logOutput ?? (s => { });

            var result = new CuttingPlaneResult();
            safeLogOutput("Starting Cutting Plane Algorithm (Gomory Cuts)\n");
            safeLogOutput($"Objective: {model.ObjectiveType}\n");

            // Ensure IntegerIndices is not null
            if (model.IntegerIndices == null)
            {
                model.IntegerIndices = new List<int>();
            }

            if (model.IntegerIndices.Count > 0)
            {
                safeLogOutput($"Variables to be integer: {string.Join(", ", model.IntegerIndices.Select(i => $"x{i + 1}"))}\n\n");
            }
            else
            {
                safeLogOutput("No integer variables specified. Using all variables as integer.\n");
                model.IntegerIndices = Enumerable.Range(0, model.ObjectiveCoefficients.Count).ToList();
            }

            // Create initial tableau using SimplexSolver
            var (tableau, constraintTypes) = _simplexSolver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            int iteration = 0;
            bool optimalIntegerSolution = false;

            safeLogOutput("Initial Tableau:\n");
            safeLogOutput(_canonicalForm.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

            // Store initial tableau
            result.Iterations.Add(new CuttingPlaneIteration
            {
                Iteration = iteration,
                Tableau = (double[,])tableau.Clone(),
                Message = "Initial Tableau"
            });

            while (!optimalIntegerSolution && iteration < 100) // Safety limit on iterations
            {
                iteration++;
                safeLogOutput($"\n--- Cutting Plane Iteration {iteration} ---\n");

                // Solve the LP relaxation using dual simplex
                tableau = _dualSimplexSolver.SolveDual(tableau, constraintTypes,
                    msg => {
                        if (msg.Contains("Iteration") || msg.Contains("Optimal"))
                            safeLogOutput(msg);
                    },
                    numVariables, numConstraints, model.ObjectiveType);

                // Check if solution is integer for required variables
                if (IsIntegerSolution(tableau, model.IntegerIndices, numVariables, numConstraints))
                {
                    optimalIntegerSolution = true;
                    safeLogOutput("\nInteger optimal solution found!\n");
                    result.SolutionFound = true;
                    break;
                }

                // Generate and add Gomory cut
                int cutRow = FindMostFractionalRow(tableau, model.IntegerIndices, numVariables, numConstraints);
                if (cutRow == -1)
                {
                    safeLogOutput("No suitable row found for generating cut\n");
                    result.SolutionFound = false;
                    break;
                }

                safeLogOutput($"Generating Gomory cut from row {cutRow}\n");
                tableau = AddGomoryCut(tableau, cutRow, numVariables, numConstraints);
                numConstraints++; // We adding a new constraint

                // Update constraintTypes for the new cut
                constraintTypes.Add("<=");

                safeLogOutput($"Tableau after adding cut:\n");
                safeLogOutput(_canonicalForm.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

                // Store iteration data
                result.Iterations.Add(new CuttingPlaneIteration
                {
                    Iteration = iteration,
                    Tableau = (double[,])tableau.Clone(),
                    Message = $"Added Gomory cut from row {cutRow}"
                });
            }

            if (optimalIntegerSolution)
            {
                result.FinalTableau = tableau;
                result.ObjectiveValue = ExtractObjectiveValue(tableau);
                result.VariableValues = ExtractVariableValues(tableau, numVariables, numConstraints);
                PrintSolution(tableau, numVariables, numConstraints, safeLogOutput);
            }
            else
            {
                safeLogOutput("\nCutting plane algorithm terminated without finding integer solution\n");
            }

            return result;
        }

        // The rest of the methods remain the same
        private bool IsIntegerSolution(double[,] tableau, List<int> integerIndices, int numVariables, int numConstraints)
        {
            if (integerIndices == null || integerIndices.Count == 0) return true;

            int rhsCol = tableau.GetLength(1) - 1;

            foreach (int varIndex in integerIndices)
            {
                // Check if this variable is basic
                for (int row = 1; row <= numConstraints; row++)
                {
                    if (Math.Abs(tableau[row, varIndex] - 1) < Tolerance)
                    {
                        double value = tableau[row, rhsCol];
                        if (Math.Abs(value - Math.Round(value)) > Tolerance)
                            return false;
                        break;
                    }
                }
            }

            return true;
        }

        private int FindMostFractionalRow(double[,] tableau, List<int> integerIndices, int numVariables, int numConstraints)
        {
            if (integerIndices == null || integerIndices.Count == 0) return -1;

            int rhsCol = tableau.GetLength(1) - 1;
            double maxFractionality = 0;
            int selectedRow = -1;

            for (int row = 1; row <= numConstraints; row++)
            {
                // Find which variable is basic in this row
                for (int col = 0; col < numVariables; col++)
                {
                    if (Math.Abs(tableau[row, col] - 1) < Tolerance && integerIndices.Contains(col))
                    {
                        double value = tableau[row, rhsCol];
                        double fractionality = Math.Min(value - Math.Floor(value), Math.Ceiling(value) - value);

                        if (fractionality > maxFractionality)
                        {
                            maxFractionality = fractionality;
                            selectedRow = row;
                        }
                        break;
                    }
                }
            }

            return selectedRow;
        }

        private double[,] AddGomoryCut(double[,] tableau, int sourceRow, int numVariables, int numConstraints)
        {
            int oldRows = tableau.GetLength(0);
            int oldCols = tableau.GetLength(1);
            int rhsCol = oldCols - 1;

            // Create new tableau with one additional row and column
            double[,] newTableau = new double[oldRows + 1, oldCols + 1];

            // Copy old tableau
            for (int i = 0; i < oldRows; i++)
            {
                for (int j = 0; j < oldCols; j++)
                {
                    newTableau[i, j] = tableau[i, j];
                }
            }

            // Add new slack variable column (initialize to 0)
            for (int i = 0; i < oldRows; i++)
            {
                newTableau[i, oldCols] = 0;
            }

            // Generate Gomory cut coefficients
            for (int j = 0; j < oldCols; j++)
            {
                double value = tableau[sourceRow, j];
                double fractionalPart = value - Math.Floor(value);
                newTableau[oldRows, j] = -fractionalPart;
            }

            // Set coefficient for new slack variable to 1
            newTableau[oldRows, oldCols] = 1;

            // Set RHS for new row
            double rhsValue = tableau[sourceRow, rhsCol];
            double fractionalRhs = rhsValue - Math.Floor(rhsValue);
            newTableau[oldRows, rhsCol + 1] = -fractionalRhs;

            return newTableau;
        }

        private double ExtractObjectiveValue(double[,] tableau)
        {
            int rhsCol = tableau.GetLength(1) - 1;
            return tableau[0, rhsCol];
        }

        private double[] ExtractVariableValues(double[,] tableau, int numVariables, int numConstraints)
        {
            int rhsCol = tableau.GetLength(1) - 1;
            double[] values = new double[numVariables];

            for (int j = 0; j < numVariables; j++)
            {
                values[j] = 0;

                // Check if this variable is basic
                for (int i = 1; i <= numConstraints; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1) < Tolerance)
                    {
                        values[j] = tableau[i, rhsCol];
                        break;
                    }
                }
            }

            return values;
        }

        private void PrintSolution(double[,] tableau, int numVariables, int numConstraints, Action<string> logOutput)
        {
            int rhsCol = tableau.GetLength(1) - 1;

            logOutput?.Invoke("Optimal Solution:\n");
            logOutput?.Invoke($"Objective Value: {tableau[0, rhsCol]:F3}\n\n");

            logOutput?.Invoke("Decision Variables:\n");
            for (int j = 0; j < numVariables; j++)
            {
                double value = 0;

                // Check if this variable is basic
                for (int i = 1; i <= numConstraints; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1) < Tolerance)
                    {
                        value = tableau[i, rhsCol];
                        break;
                    }
                }

                logOutput?.Invoke($"x{j + 1} = {value:F3}\n");
            }

            logOutput?.Invoke("\nSlack Variables:\n");
            for (int j = numVariables; j < numVariables + numConstraints; j++)
            {
                double value = 0;

                // Check if this slack variable is basic
                for (int i = 1; i <= numConstraints; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1) < Tolerance)
                    {
                        value = tableau[i, rhsCol];
                        break;
                    }
                }

                logOutput?.Invoke($"s{j - numVariables + 1} = {value:F3}\n");
            }
        }
    }

    internal class CuttingPlaneResult  
    {
        public bool SolutionFound { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; } = Array.Empty<double>();
        public double[,] FinalTableau { get; set; } = new double[0, 0];
        public List<CuttingPlaneIteration> Iterations { get; set; } = new List<CuttingPlaneIteration>();
    }

    internal class CuttingPlaneIteration  
    {
        public int Iteration { get; set; }
        public double[,] Tableau { get; set; } = new double[0, 0];
        public string Message { get; set; } = string.Empty;
    }
}

