using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal class SimplexSolver
    {
        private List<int> _lastBasisIndices;
        public (double[,], List<string>) CreateTableau(LPModel model) // Takes the parsed model and transforms it to its standard form which is then written in Tableaur form to be used by the primal simplex.
        {
            //Saves the parsed objective function,constraints and sign restrictions in variables to be used by the method

            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            int width = numVariables + numConstraints + 1;
            int height = numConstraints + 1;
            double[,] tableau = new double[height, width];
            var constraintTypes = new List<string>();

            // Converts the Objective coefficents to negative values if it is a Max model.
            for (int j = 0; j < numVariables; j++)
            {
                double coeff = model.ObjectiveCoefficients[j];
                tableau[0, j] = model.ObjectiveType.Equals("Max", StringComparison.OrdinalIgnoreCase)
                    ? -coeff   // maximize → negatife
                    : coeff;   // minimize → positive
            }

            // Converts the constraints to their standard form by adding slack or surplus variables
            for (int i = 0; i < numConstraints; i++)
            {
                string constraint = model.Constraints[i];

                // Parse coefficients for decision variables
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*\.?\d*)\s*\*?\s*x(\d+)");
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    int varIndex = int.Parse(match.Groups[2].Value) - 1;

                    double coeff = string.IsNullOrEmpty(coeffStr) || coeffStr == "+" ? 1.0 :
                                   coeffStr == "-" ? -1.0 : double.Parse(coeffStr);

                    tableau[i + 1, varIndex] = coeff;
                }

                // Detect constraint type
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
                    tableau[i + 1, slackCol] = 1.0; // surplus variable
                    for (int j = 0; j < numVariables; j++)
                        tableau[i + 1, j] *= -1;

                    tableau[i + 1, width - 1] *= -1; // flip RHS
                    constraintTypes.Add(">=");
                }
                else
                {
                    // default to slack if unknown
                    tableau[i + 1, slackCol] = 1.0;
                    constraintTypes.Add("<=");
                }

                // Parse RHS
                var rhsMatch = Regex.Match(constraint, @"-?\d*\.?\d+\s*$");
                if (rhsMatch.Success)
                    tableau[i + 1, width - 1] = double.Parse(rhsMatch.Value);
            }

            return (tableau, constraintTypes); 
        }
        public double[,] Solve(double[,] tableau, List<string> constraintTypes, Action<string> logOutput, int numVariables, int numConstraints, string objectiveType) // The method used to execute the primal simplex.
        {
            int[] basis = new int[numConstraints];
            int iteration = 1;
            var headers = new CanonicalForm();

            while (PerformIteration(tableau, numConstraints, tableau.GetLength(1), basis, objectiveType, logOutput)) // Calls the iteration method to perform pivoting logic.
            {
                logOutput($"\r\nIteration {iteration++}:\r\n");
                logOutput(headers.TableauToString(tableau, numVariables, numConstraints, constraintTypes)); // Labels rows and columns with headers
            }
            _lastBasisIndices = basis.ToList();
            logOutput("\r\nOptimal solution reached.\r\n");
            return tableau; // returns the optimal solution

        }
        public List<int> GetBasicIndices()
        {
            return _lastBasisIndices ?? new List<int>();
        }

        private bool PerformIteration(double[,] tableau, int numConstraints,int numCols, int[] basis, string objectiveType, Action<string> logOutput)
        {
            int pivotCol = -1;
            double mostNegative = 0;
            
            // Reads through the columns to get the pivot column. If max then the pivot column is the most negative objective coeffiecent, if Min then most positive
            for (int j = 0; j<numCols -1; j++)
            {
                if (objectiveType.Equals("Max", StringComparison.OrdinalIgnoreCase))
                {
                    if (tableau[0, j] < mostNegative)
                    {
                        mostNegative = tableau[0, j];
                        pivotCol = j;
                    }
                }
                else if (objectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                {
                    if (tableau[0, j] > mostNegative)
                    {
                        mostNegative = tableau[0, j];
                        pivotCol = j;
                    }
                }
            }
            if (pivotCol == -1)
            {
                return false;
            }

            // Gets the pivot row by dividing the rhs values with the values in the pivot columns and choosing the smallest positive ratios.
            int pivotRow = -1;
            double minRatio = double.MaxValue;
            for (int i = 1; i <= numConstraints; i++)
            {
                double pivotVal = tableau[i, pivotCol];
                if (pivotVal > 1e-9)
                {
                    double ratio = tableau[i, numCols - 1] / pivotVal;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }
            // Unbounded solution
            if (pivotRow == -1)
            {
                logOutput("\r\nSolution is Unbounded.\r\n");
                return false; // Stop iterations
            }

            // gets the pivoting point where the pivot row and columns interconnnects, And then diveds each column with that value.
            basis[pivotRow - 1] = pivotCol;
            double pivotElement = tableau[pivotRow, pivotCol];
            for (int j = 0; j < numCols; j++)
            {
                tableau[pivotRow, j] /= pivotElement;
            }
            for (int i = 0; i <= numConstraints; i++)
            {
                if (i == pivotRow) continue;
                double factor = tableau[i, pivotCol];
                for (int j = 0; j < numCols; j++)
                {
                    tableau[i, j] -= factor * tableau[pivotRow, j];
                }
            }
            return true; //Returns the iterated table
        }    
    }
}

