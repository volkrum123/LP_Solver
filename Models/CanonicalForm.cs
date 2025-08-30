using System;
using System.Collections.Generic;
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

        /*public string TableauToString(double[,] tableau, int numVariables, int numConstraints, List<string>? constraintTypes = null)
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
        }*/
        public string TableauToString(double[,] tableau, int numVariables, int numConstraints, List<string> constraintTypes)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int rhsColumn = cols - 1; // RHS is always the last column

            // Column headers: x1,x2,...,slack/surplus, RHS
            var colHeaders = new List<string>();
            for (int i = 0; i < numVariables; i++)
                colHeaders.Add("x" + (i + 1));

            for (int i = 0; i < numConstraints; i++)
            {
                if (constraintTypes != null && i < constraintTypes.Count && constraintTypes[i] == ">=")
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
                    string value = tableau[i, j].ToString("0.###").Replace("-0", "0");
                    sb.Append(value.PadLeft(8));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
