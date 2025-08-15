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
        public double[,] CreateTableau(LPModel model)
        {
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            int numSlack = numConstraints;

            int width = numVariables + numSlack + 1;
            int height = numConstraints + 1;

            double[,] tableau = new double[height,width];

            for (int j = 0; j < numVariables; j++)
            {
                double coeff = model.ObjectiveCoefficients[j];

                if (model.ObjectiveType.Equals("Max", StringComparison.OrdinalIgnoreCase))
                {
                    tableau[0, j] = -coeff; // Max → negate for simplex
                }
                else if (model.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                {
                    tableau[0, j] = coeff;  // Min → keep as-is, pivot rule will differ
                }
                //tableau[0, j] = -model.ObjectiveCoefficients[j];
            }
            for(int i = 0; i < numConstraints; i++)
            {
                string constraint = model.Constraints[i];
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*\.?\d*)\s*\*?\s*x(\d+)");
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    string varIndexStr = match.Groups[2].Value;
                    int varIndex = int.Parse(varIndexStr) - 1;
                    double coeff = string.IsNullOrEmpty(coeffStr) || coeffStr == "+" ? 1.0:
                        coeffStr == "-" ? -1.0 : double.Parse(coeffStr);
                    tableau[i + 1, varIndex] = coeff;
                }
                tableau[i + 1, numVariables + i] = 1.0;

                var rhsMatch = Regex.Match(constraint, @"-?\d*\.?\d+\s*$");
                if (rhsMatch.Success)
                {
                    tableau[i + 1, width - 1] = double.Parse(rhsMatch.Value);
                }
            }
            return tableau;
        }
        public void Solve(double[,] tableau, string objectiveType, Action<string> logOutput, int numVariables, int numConstraints)
        {
            int[] basis = new int[numConstraints];
            int iteration = 1;

            while (PerformIteration(tableau, numConstraints, tableau.GetLength(1), basis, objectiveType))
            {
                logOutput($"\r\nIteration {iteration++}:\r\n");
                logOutput(TableauToString(tableau, numVariables, numConstraints));
            }
            logOutput("\r\nOptimal solution reached.\r\n");
        }

        private bool PerformIteration(double[,] tableau, int numConstraints,int numCols, int[] basis, string objectiveType)
        {
            int pivotCol = -1;
            double mostNegative = 0;

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
            if(pivotCol == -1)
            {
                return false;
            }

            int pivotRow = -1;
            double minRatio = double.MaxValue;
            for(int i =1; i <= numConstraints; i++)
            {
                double pivotVal = tableau[i,pivotCol];
                if(pivotVal > 1e-9)
                {
                    double ratio = tableau[i,numCols - 1]/pivotVal;
                    if(ratio < minRatio)
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
                for(int j = 0;j < numCols; j++)
                {
                    tableau[i,j] -= factor * tableau[pivotRow,j];
                }
            }
            return true;
        }

        public string TableauToString(double[,] tableau, int numVariables, int numConstraints)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            // Generate column headers: x1,x2,...,s1,s2,...,RHS
            var colHeaders = new List<string>();
            for (int i = 0; i < numVariables; i++)
                colHeaders.Add("x" + (i + 1));
            for (int i = 0; i < numConstraints; i++)
                colHeaders.Add("s" + (i + 1));
            colHeaders.Add("RHS");

            var sb = new StringBuilder();

            // Header row
            sb.Append("     ");
            foreach (var col in colHeaders)
                sb.Append(col.PadLeft(8));
            sb.AppendLine();

            // Data rows
            for (int i = 0; i < rows; i++)
            {
                string rowHeader = (i == 0) ? "z" : $"C{i}";
                sb.Append(rowHeader.PadRight(5));
                for (int j = 0; j < cols; j++)
                    sb.Append(tableau[i, j].ToString("0.###").PadLeft(8));
                sb.AppendLine();
            }

            return sb.ToString();
           
        }
    }
}

