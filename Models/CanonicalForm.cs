using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class CanonicalForm
    {
        public string ConvertToCanonicalFormSequential(LPModel model)
        {
            int numVariables = model.ObjectiveCoefficients.Count;
            int variableCounter = 1; // Counter for slack/artificial vars across all constraints

            var sb = new StringBuilder();
            sb.AppendLine("Canonical Form:");

            // --- Objective function ---
            string obj = "Z ";
            for (int j = 0; j < numVariables; j++)
            {
                double coeff = model.ObjectiveCoefficients[j];
                obj += model.ObjectiveType.Equals("Max", StringComparison.OrdinalIgnoreCase) ? $"- {coeff}x{j + 1} " : $"+ {coeff}x{j + 1} ";
            }
            obj += "= 0";
            sb.AppendLine(obj);
            sb.AppendLine();

            // --- Constraints ---
            sb.AppendLine("Subject to:");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                string constraint = model.Constraints[i];

                // Extract RHS
                var rhsMatch = Regex.Match(constraint, @"-?\d*\.?\d+\s*$");
                double rhs = rhsMatch.Success ? double.Parse(rhsMatch.Value, System.Globalization.CultureInfo.InvariantCulture) : 0;

                // Extract LHS coefficients
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*\.?\d*)\s*\*?\s*x(\d+)");
                double[] coeffs = new double[numVariables];
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    int varIndex = int.Parse(match.Groups[2].Value) - 1;
                    double coeff = string.IsNullOrEmpty(coeffStr) || coeffStr == "+"
                        ? 1.0
                        : coeffStr == "-" ? -1.0 : double.Parse(coeffStr, System.Globalization.CultureInfo.InvariantCulture);
                    coeffs[varIndex] = coeff;
                }

                // --- Build constraint ---
                var constr = new StringBuilder();
                if (constraint.Contains("<="))
                {
                    // Slack variable
                    for (int j = 0; j < numVariables; j++)
                        if (coeffs[j] != 0) constr.Append($"{coeffs[j]}x{j + 1} ");
                    constr.Append($"+ 1s{variableCounter} = {rhs}");
                    variableCounter++;
                }
                else if (constraint.Contains(">="))
                {
                    // Multiply by -1, add artificial variable
                    rhs = -rhs;
                    for (int j = 0; j < numVariables; j++)
                        if (coeffs[j] != 0) constr.Append($"{-coeffs[j]}x{j + 1} ");
                    constr.Append($"+ 1e{variableCounter} = {rhs}");
                    variableCounter++;
                }

                sb.AppendLine(constr.ToString());
            }

            // --- Non-negativity ---
            sb.AppendLine();
            sb.Append("Where: ");
            for (int j = 0; j < numVariables; j++)
                sb.Append($"x{j + 1}, ");
            for (int v = 1; v < variableCounter; v++)
            {
                sb.Append($"s{v}, ");
                sb.Append($"e{v}, ");
            }
            sb.Append(">= 0");

            return sb.ToString();
        }

        public string TableauToString(double[,] tableau, int numVariables, int numConstraints, List<string>? constraintTypes = null)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            // Column headers: x1,x2,...,slack/surplus, RHS
            var colHeaders = new List<string>();
            for (int i = 0; i < numVariables; i++)
                colHeaders.Add("x" + (i + 1));

            for (int i = 0; i < numConstraints; i++)
            {
                // Use constraintTypes list if provided
                if (constraintTypes != null && constraintTypes[i] == ">=")
                    colHeaders.Add("e" + (i + 1)); // surplus variable
                else
                    colHeaders.Add("s" + (i + 1)); // slack variable
            }

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
                {
                    // Avoid printing -0
                    string value = tableau[i, j].ToString("0.###").Replace("-0", "0");
                    sb.Append(value.PadLeft(8));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // Build knapsack tableau with ONLY x-columns + RHS.
        // Row 0: z row (canonical for Max => -values), Row 1: single capacity constraint (weights).
        public (double[,] tableau, string[] colHeaders, string[] rowHeaders)
            BuildKnapsackXOnlyTableau(KnapsackModel ks, bool negateObjForMax = true)
        {
            int n = ks.Items.Count;
            int rows = 2;         // z + C1
            int cols = n + 1;     // x1..xn + RHS
            var T = new double[rows, cols];

            // z row
            for (int j = 0; j < n; j++)
                T[0, j] = negateObjForMax ? -ks.Items[j].Value : ks.Items[j].Value;
            T[0, cols - 1] = 0;

            // C1 row
            for (int j = 0; j < n; j++)
                T[1, j] = ks.Items[j].Weight;
            T[1, cols - 1] = ks.Capacity;

            var colsHdr = Enumerable.Range(1, n).Select(i => $"x{i}").Concat(new[] { "RHS" }).ToArray();
            var rowsHdr = new[] { "z", "C1" };
            return (T, colsHdr, rowsHdr);
        }

        public (double[,] tableau, string[] colHeaders, string[] rowHeaders)
            BuildKnapsackXOnlyTableauWithDecisions(
                KnapsackModel ks,
                IEnumerable<(int index, int decision)> decisions,
                bool negateObjForMax = true)
        {
            var (T, colsHdr, rowsHdr) = BuildKnapsackXOnlyTableau(ks, negateObjForMax);
            int rhs = colsHdr.Length - 1;

            foreach (var (idx, dec) in decisions)
            {
                if (dec == 1)
                {
                    T[0, rhs] += ks.Items[idx].Value;    // z RHS
                    T[1, rhs] -= ks.Items[idx].Weight;   // capacity RHS
                }
                // zero the x-column (fixed)
                T[0, idx] = 0;
                T[1, idx] = 0;
            }
            return (T, colsHdr, rowsHdr);
        }

        public string TableauToStringCustom(
            double[,] T,
            IReadOnlyList<string> colHeaders,
            IReadOnlyList<string> rowHeaders,
            string title = "Initial Tableau:",
            int pad = 8,
            int decimals = 3)
        {
            int rows = T.GetLength(0);
            int cols = T.GetLength(1);

            var widths = colHeaders.Select(h => Math.Max(h.Length, pad)).ToArray();

            string fmt(double v)
            {
                var s = Math.Round(v, decimals).ToString("0.###", CultureInfo.InvariantCulture);
                return s == "-0" ? "0" : s;
            }

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    widths[c] = Math.Max(widths[c], fmt(T[r, c]).Length);

            var sb = new StringBuilder();
            sb.AppendLine(title);

            sb.Append("     ");
            for (int c = 0; c < cols; c++) sb.Append(colHeaders[c].PadLeft(widths[c]));
            sb.AppendLine();

            for (int r = 0; r < rows; r++)
            {
                string rowLabel = (r < rowHeaders.Count) ? rowHeaders[r] : $"R{r}";
                sb.Append(rowLabel.PadRight(5));
                for (int c = 0; c < cols; c++) sb.Append(fmt(T[r, c]).PadLeft(widths[c]));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
