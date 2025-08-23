using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class RevisedPrimal
    {
        public double[] Solve(LPModel model, Action<string> logOutput)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            logOutput("Parsing constraints and building matrices...\r\n");

            // Convert LPModel to numeric matrices
            ParseConstraints(model, out double[,] A, out double[] b);

            double[] c = model.ObjectiveCoefficients.ToArray();
            bool isMin = model.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase);
            if (isMin)
                for (int i = 0; i < c.Length; i++)
                    c[i] = -c[i];

            int m = b.Length;
            int n = c.Length;

            // Initialize slack basis
            List<int> basis = Enumerable.Range(n, m).ToList();
            List<int> nonBasis = Enumerable.Range(0, n).ToList();

            // Extend A with slack variables
            double[,] A_ext = new double[m, n + m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) A_ext[i, j] = A[i, j];
                A_ext[i, n + i] = 1.0; // slack variable
            }

            // Extend c
            double[] c_ext = new double[n + m];
            for (int i = 0; i < n; i++) c_ext[i] = c[i];

            // Initial basis solution
            double[] xB = SafeSolve(GetBasisMatrix(A_ext, basis), b);

            int iter = 0;
            while (true)
            {
                iter++;

                double[] cB = basis.Select(i => c_ext[i]).ToArray();
                double[] pi = SafeSolve(Matrix.Transpose(GetBasisMatrix(A_ext, basis)), cB);

                // Compute reduced costs
                double[] reducedCosts = new double[c_ext.Length];
                for (int j = 0; j < c_ext.Length; j++)
                {
                    if (!basis.Contains(j))
                    {
                        double[] a_j = GetColumn(A_ext, j);
                        reducedCosts[j] = c_ext[j] - Matrix.Dot(pi, a_j);
                    }
                }

                // Find entering variable
                double maxReducedCost = 0;
                int enteringIndex = -1;
                for (int k = 0; k < nonBasis.Count; k++)
                {
                    int j = nonBasis[k];
                    if (reducedCosts[j] > maxReducedCost + 1e-9)
                    {
                        maxReducedCost = reducedCosts[j];
                        enteringIndex = k;
                    }
                }

                int enteringVar = enteringIndex != -1 ? nonBasis[enteringIndex] : -1;

                // Ratio test to find leaving variable
                double[] d = enteringVar != -1 ? SafeSolve(GetBasisMatrix(A_ext, basis), GetColumn(A_ext, enteringVar)) : null;
                double theta = double.MaxValue;
                int leavingIndex = -1;

                if (enteringVar != -1)
                {
                    for (int i = 0; i < m; i++)
                    {
                        if (d[i] > 1e-9)
                        {
                            double ratio = xB[i] / d[i];
                            if (ratio < theta)
                            {
                                theta = ratio;
                                leavingIndex = i;
                            }
                        }
                    }
                }

                int leavingVar = leavingIndex != -1 ? basis[leavingIndex] : -1;

                // Print iteration
                PrintIterationTableau(iter, basis, A_ext, xB, reducedCosts, c_ext, enteringVar, leavingVar, n, logOutput);

                // Check for optimality
                if (enteringVar == -1)
                {
                    double[] fullSolution = BuildFullSolution(xB, basis, n + m);
                    double Z = 0;
                    for (int i = 0; i < n; i++)
                        Z += model.ObjectiveCoefficients[i] * fullSolution[i];

                    logOutput("\r\nOptimal solution found:\r\n");
                    DisplaySolution(fullSolution, model.NumVariables, logOutput);
                    logOutput($"\r\nOptimal objective value Z* = {Z}\r\n");
                    return fullSolution;
                }

                if (leavingVar == -1)
                {
                    logOutput("Unbounded problem!\r\n");
                    return null;
                }

                // Update solution
                for (int i = 0; i < m; i++)
                    xB[i] -= theta * d[i];
                xB[leavingIndex] = theta;

                // Update basis
                basis[leavingIndex] = enteringVar;
                nonBasis[enteringIndex] = leavingVar;
            }
        }

        private double[] SafeSolve(double[,] M, double[] v)
        {
            // Gaussian elimination with partial pivoting
            int n = M.GetLength(0);
            double[,] A = (double[,])M.Clone();
            double[] b = (double[])v.Clone();

            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                    if (Math.Abs(A[k, i]) > Math.Abs(A[maxRow, i]))
                        maxRow = k;

                if (maxRow != i)
                {
                    for (int j = 0; j < n; j++)
                    {
                        double tmp = A[i, j];
                        A[i, j] = A[maxRow, j];
                        A[maxRow, j] = tmp;
                    }
                    double tmpb = b[i]; b[i] = b[maxRow]; b[maxRow] = tmpb;
                }

                double diag = A[i, i];
                if (Math.Abs(diag) < 1e-12) diag = 1e-12;
                for (int j = i; j < n; j++) A[i, j] /= diag;
                b[i] /= diag;

                for (int k = 0; k < n; k++)
                {
                    if (k == i) continue;
                    double factor = A[k, i];
                    for (int j = i; j < n; j++)
                        A[k, j] -= factor * A[i, j];
                    b[k] -= factor * b[i];
                }
            }
            return b;
        }

        private void PrintIterationTableau(int iter, List<int> basis, double[,] A_ext, double[] xB,
            double[] reducedCosts, double[] c_ext, int enteringVar, int leavingVar, int nOriginal,
            Action<string> logOutput)
        {
            logOutput?.Invoke($"\r\n--- Iteration {iter} ---\r\n");
            string VarName(int idx) => idx < nOriginal ? $"x{idx + 1}" : $"s{idx - nOriginal + 1}";

            logOutput("Basis: " + string.Join(", ", basis.Select(VarName)) + "\r\n");

            int m = xB.Length;
            int n = c_ext.Length;

            double[,] tableauFull = new double[m, n + 1];
            for (int j = 0; j < n; j++)
            {
                double[] col = basis.Contains(j)
                    ? Enumerable.Range(0, m).Select(i => i == basis.IndexOf(j) ? 1.0 : 0.0).ToArray()
                    : SafeSolve(GetBasisMatrix(A_ext, basis), GetColumn(A_ext, j));

                for (int i = 0; i < m; i++)
                    tableauFull[i, j] = col[i];
            }

            for (int i = 0; i < m; i++)
                tableauFull[i, n] = xB[i];

            var header = string.Join("\t", Enumerable.Range(0, n).Select(VarName)) + "\tRHS";
            logOutput(header + "\r\n");

            for (int i = 0; i < m; i++)
            {
                var rowValues = new List<string>();
                for (int j = 0; j < n; j++)
                    rowValues.Add(tableauFull[i, j].ToString("F3"));
                rowValues.Add(tableauFull[i, n].ToString("F3"));
                logOutput(string.Join("\t", rowValues) + "\r\n");
            }

            logOutput("Reduced costs:\r\n");
            for (int j = 0; j < reducedCosts.Length; j++)
                if (Math.Abs(c_ext[j]) > 1e-9 || Math.Abs(reducedCosts[j]) > 1e-9)
                    logOutput($"\tc̅[{VarName(j)}] = {reducedCosts[j]:F3}\r\n");

            if (enteringVar != -1 && leavingVar != -1)
                logOutput?.Invoke($"Entering variable: {VarName(enteringVar)}, Leaving variable: {VarName(leavingVar)}\r\n");

            double z = Matrix.Dot(basis.Select(i => c_ext[i]).ToArray(), xB);
            logOutput?.Invoke($"Current objective value Z = {z:F3}\r\n");
        }

        private void ParseConstraints(LPModel model, out double[,] A, out double[] b)
        {
            int m = model.Constraints.Count;
            int n = model.NumVariables;
            A = new double[m, n];
            b = new double[m];

            for (int i = 0; i < m; i++)
            {
                string constraint = model.Constraints[i];
                if (!constraint.Contains("<="))
                    throw new Exception("Only <= constraints are supported.");

                string[] parts = constraint.Split("<=");
                string left = parts[0];
                string right = parts[1];

                b[i] = double.Parse(right.Trim());

                for (int j = 0; j < n; j++)
                {
                    string varName = $"x{j + 1}";
                    int index = left.IndexOf(varName);
                    if (index >= 0)
                    {
                        int coeffStart = left.LastIndexOf(' ', index - 1);
                        string coeffStr = left.Substring(coeffStart + 1, index - coeffStart - 1).Trim();
                        A[i, j] = string.IsNullOrEmpty(coeffStr) ? 1.0 : double.Parse(coeffStr);
                    }
                    else
                        A[i, j] = 0.0;
                }
            }
        }

        private double[,] GetBasisMatrix(double[,] A, List<int> basis)
        {
            int m = A.GetLength(0);
            double[,] B = new double[m, m];
            for (int j = 0; j < m; j++)
            {
                double[] col = GetColumn(A, basis[j]);
                for (int i = 0; i < m; i++)
                    B[i, j] = col[i];
            }
            return B;
        }

        private double[] GetColumn(double[,] M, int col)
        {
            int rows = M.GetLength(0);
            double[] result = new double[rows];
            for (int i = 0; i < rows; i++)
                result[i] = M[i, col];
            return result;
        }

        private double[] BuildFullSolution(double[] xB, List<int> basis, int totalVars)
        {
            double[] solution = new double[totalVars];
            for (int i = 0; i < basis.Count; i++)
                solution[basis[i]] = xB[i];
            return solution;
        }

        private void DisplaySolution(double[] solution, int numVariables, Action<string> logOutput)
        {
            logOutput("Variable values:\r\n");
            for (int i = 0; i < numVariables; i++)
                logOutput($"x{i + 1} = {solution[i]}\r\n");
        }
    }

    internal static class Matrix
    {
        public static double[] Multiply(double[,] M, double[] v)
        {
            int rows = M.GetLength(0);
            int cols = M.GetLength(1);
            double[] result = new double[rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i] += M[i, j] * v[j];
            return result;
        }

        public static double[,] Transpose(double[,] M)
        {
            int rows = M.GetLength(0);
            int cols = M.GetLength(1);
            double[,] result = new double[cols, rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = M[i, j];
            return result;
        }

        public static double Dot(double[] v1, double[] v2)
        {
            double sum = 0;
            for (int i = 0; i < v1.Length; i++)
                sum += v1[i] * v2[i];
            return sum;
        }
    }
    
}
