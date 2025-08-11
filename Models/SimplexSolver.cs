using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal class SimplexSolver
    {
        public double[,] CreateTableau(LPModel model)
        {
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            int numSlack = numConstraints;

            int width = numVariables + numSlack + 1;
            int height = numConstraints + 1;

            double[,] tableau = new double[width, height];

            for (int j = 0; j < numVariables; j++)
            {
                tableau[0, j] = -model.ObjectiveCoefficients[j];
            }
            for(int i = 0; i < numConstraints; i++)
            {
                string constraint = model.Constraints[i];
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*)\s*\*?\s*x(\d+)");
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    string varIndexStr = match.Groups[2].Value;
                    int varIndex = int.Parse(varIndexStr) - 1;
                    int coeff = string.IsNullOrEmpty(coeffStr) || coeffStr == "+" ? 1:
                        coeffStr == "-" ? -1 : int.Parse(coeffStr);
                    tableau[i + 1, varIndex] = coeff;
                }
                tableau[i + 1, numVariables + i] = 1;

                var rhsMatch = Regex.Match(constraint, @"(\d+)\s*$");
                if (rhsMatch.Success)
                {
                    tableau[i + 1, width - 1] = double.Parse(rhsMatch.Value);
                }
            }
            return tableau;
        }

        public void Solve(double[,] tableau, Action<string> log)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int numConstraints = rows - 1;

            int[] basis = new int[numConstraints];
            for (int i = 0; i < numConstraints; i++)
            {
                basis[i] = cols - numConstraints - 1 + i;
            }
            int iteration = 1;
            while (PerformIteration(tableau,numConstraints, cols, basis))
            {
                log($"\r\nIteration {iteration++}:\r\n{TableauToString(tableau)}");
            }
            log("\r\nOptimal solution reached.\r\n");
        }

        private bool PerformIteration(double[,] tableau, int numConstraints,int numCols, int[] basis)
        {
            int pivotCol = -1;
            double mostNegative = 0;

            for (int j = 0; j<numCols -1; j++)
            {
                if (tableau[0, j] < mostNegative)
                {
                    mostNegative = tableau[0, j];
                    pivotCol = j;
                }
            }
            if(pivotCol == -1)
            {
                return false;
            }

            int pivotRow = -1;
            double minRatio = double.MaxValue;
            for(int i =1; i <= numConstraints; i++)
            {
                double pivotVal = tableau[i,pivotCol];
                if(pivotVal > 0)
                {
                    double ratio = tableau[i,numCols - 1]/pivotVal;
                    if(ratio > minRatio)
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }
            if (pivotRow == -1) throw new Exception("Unbounded solution");
            basis[pivotRow - 1] = pivotCol;

            double pivotElement = tableau[pivotRow, pivotCol];
            for(int j = 0; j < numCols; j++)
            {
                tableau[pivotRow,j] /= pivotElement; 
            }
            for(int i = 0;i <= numConstraints; i++)
            {
                if (i == pivotRow) continue;
                double factor = tableau[i, pivotCol];
                for(int j = 0;j <= numCols; j++)
                {
                    tableau[i,j] -= factor * tableau[pivotRow,j];
                }
            }
            return true;
        }

        public string TableauToString(double[,] tableau)
        {
            var sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            for(int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append($"{tableau[i, j],6:F2}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
