using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class DuelSimplexSolver
    {
        public (double[,],List<string>) CreateTableau(LPModel model)
        {
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            int width = numVariables + numConstraints + 1; // variables + slack/surplus + RHS
            int height = numConstraints + 1;

            double[,] tableau = new double[height, width];
            var constraintTypes = new List<string>();

            // Objective function (row 0)
            for (int j = 0; j < numVariables; j++)
            {
                double coeff = model.ObjectiveCoefficients[j];
                tableau[0, j] = model.ObjectiveType.ToLower() == "min" ? -coeff : -coeff;
            }

            // Constraints
            for (int i = 0; i < numConstraints; i++)
            {
                string constraint = model.Constraints[i];

                // Match decimal coefficients and variable indices
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*\.?\d*)\s*\*?\s*x(\d+)");
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    string varIndexStr = match.Groups[2].Value;
                    int varIndex = int.Parse(varIndexStr) - 1;

                    double coeff = string.IsNullOrWhiteSpace(coeffStr) || coeffStr == "+" ? 1.0 :
                                   coeffStr == "-" ? -1.0 : double.Parse(coeffStr);

                    tableau[i + 1, varIndex] = coeff;
                }

                // Parse RHS
                var rhsMatch = Regex.Match(constraint, @"-?\d*\.?\d+\s*$");
                double rhs = rhsMatch.Success ? double.Parse(rhsMatch.Value) : 0;

                // Slack or surplus
                bool isLE = constraint.Contains("<=");
                bool isGE = constraint.Contains(">=");
                int slackCol = numVariables + i;

                if (isLE)
                {
                    tableau[i + 1, slackCol] = 1.0; // slack variable
                    constraintTypes.Add("<=");
                }
                else if (isGE)
                {
                    // Flip coefficients for dual simplex
                    for (int j = 0; j < numVariables; j++)
                        tableau[i + 1, j] *= -1;

                    tableau[i + 1, slackCol] = 1.0; // surplus variable now positive
                    rhs *= -1; // flip RHS
                    constraintTypes.Add(">=");
                }

                // Assign RHS
                tableau[i + 1, width - 1] = rhs;
            }

            return (tableau, constraintTypes);
        }
        public void SolveDual(double[,] tableau, List<string> constraintTypes, Action<string> logOutput, int numVariables, int numConstraints, string objectiveType)
        {
            int[] basis = new int[numConstraints];
            int iteration = 1;
            var headers = new CanonicalForm();

            while (PerformDualIteration(tableau, numConstraints, tableau.GetLength(1), basis))
            {
                logOutput($"\r\nDual Iteration {iteration++}:\r\n");
                logOutput(headers.TableauToString(tableau, numVariables, numConstraints, constraintTypes));
            }

            logOutput("\r\nDual simplex phase completed.\r\n");
            logOutput(headers.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

            // Check for primal optimality
            bool primalOptimal = true;
            for (int j = 0; j < numVariables; j++)
            {
                double coeff = tableau[0, j];

                if ((objectiveType.ToLower() == "max" && coeff < -1e-9) ||
                    (objectiveType.ToLower() == "min" && coeff > 1e-9))
                {
                    primalOptimal = false;
                    break;
                }
            }

            // If not optimal, call primal simplex
            if (!primalOptimal)
            {
                logOutput("\r\nTableau is not fully optimal — switching to primal simplex...\r\n");
                var primalSolver = new SimplexSolver();
                primalSolver.Solve(tableau, constraintTypes, logOutput, numVariables, numConstraints, objectiveType);
            }
            else
            {
                logOutput("\r\nDual simplex: Optimal solution reached.\r\n");
            }
        }

        private bool PerformDualIteration(double[,] tableau, int numConstraints, int numCols, int[] basis)
        {
            // Step 1: Pivot row → most negative RHS
            int pivotRow = -1;
            double minRHS = 1e-9;

            for (int i = 1; i <= numConstraints; i++)
            {
                double rhs = tableau[i, numCols - 1];
                if (rhs < minRHS)
                {
                    minRHS = rhs;
                    pivotRow = i;
                }
            }

            if (pivotRow == -1) return false; // All RHS ≥ 0 → done

            // Step 2: Pivot column → choose negative entries in pivot row
            int pivotCol = -1;
            double minRatio = double.MaxValue;

            for (int j = 0; j < numCols - 1; j++)
            {
                double coeff = tableau[pivotRow, j];
                if (coeff < -1e-9)
                {
                    double ratio = Math.Abs(tableau[0, j] / coeff);
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotCol = j;
                    }
                }
            }

            if (pivotCol == -1)
            {
                Console.WriteLine("Pivot row coefficients:");
                for (int j = 0; j < numCols - 1; j++)
                    Console.WriteLine($"col {j}: {tableau[pivotRow, j]}");
                throw new Exception("Dual simplex: no valid pivot column (problem may be infeasible).");
            }

            basis[pivotRow - 1] = pivotCol;

            // Step 3: Pivot operation
            double pivotElement = tableau[pivotRow, pivotCol];

            // Normalize pivot row
            for (int j = 0; j < numCols; j++)
                tableau[pivotRow, j] /= pivotElement;

            // Zero out pivot column in other rows
            for (int i = 0; i <= numConstraints; i++)
            {
                if (i == pivotRow) continue;
                double factor = tableau[i, pivotCol];
                for (int j = 0; j < numCols; j++)
                    tableau[i, j] -= factor * tableau[pivotRow, j];
            }

            return true;
        }
       
    }
}
