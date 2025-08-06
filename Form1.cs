using System.Text.RegularExpressions;

namespace LP_Solver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnParse_Click(object sender, EventArgs e)
        {
            string input = txtInput.Text;
            var parsed = ParseInput(input);

            txtOutput.Clear();
            txtOutput.AppendText($"Objective: {parsed.objectiveType}\r\n");
            txtOutput.AppendText($"Objective Coeffs: {string.Join(", ", parsed.objectiveCoeffs)}\r\n");

            for (int i = 0; i < parsed.constraints.Count; i++)
            {
                txtOutput.AppendText($"Constraint {i + 1}: {parsed.constraints[i]}\r\n");
            }

            var tableau = CreateSimplexTableau(parsed.objectiveCoeffs, parsed.constraints);
            DisplayTableau(tableau);
        }

        private (string objectiveType, List<int> objectiveCoeffs, List<string> constraints) ParseInput(string input)
        {
            string[] lines = input
                .Replace(",", "\n") // Allow comma-separated constraints
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string objectiveType = "";
            List<int> objectiveCoeffs = new List<int>();
            List<string> constraints = new List<string>();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim().ToLower();

                if (line.StartsWith("max"))
                {
                    objectiveType = "Max";
                }
                else if (line.StartsWith("min"))
                {
                    objectiveType = "Min";
                }

                if (line.Contains("z") && line.Contains("="))
                {
                    objectiveCoeffs = GetObjectiveCoefficients(line);
                }
                else if (line.Contains("<=") || line.Contains(">=") || line.Contains("="))
                {
                    constraints.Add(ParseConstraint(line));
                }
            }

            return (objectiveType, objectiveCoeffs, constraints);
        }

        private List<int> GetObjectiveCoefficients(string line)
        {
            // Match all terms like 3x1 or -x2
            var matches = Regex.Matches(line, @"([+-]?\d*)\s*\*?\s*x\d+");

            return matches.Cast<Match>().Select(m =>
            {
                string coeff = Regex.Match(m.Value, @"[+-]?\d*").Value;

                if (string.IsNullOrWhiteSpace(coeff) || coeff == "+") return 1;
                if (coeff == "-") return -1;
                return int.Parse(coeff);
            }).ToList();
        }

        private string ParseConstraint(string line)
        {
            var coeffMatches = Regex.Matches(line, @"([+-]?\d*)\s*\*?\s*x\d+");
            var varMatches = Regex.Matches(line, @"x\d+");

            List<string> terms = new List<string>();

            for (int i = 0; i < varMatches.Count; i++)
            {
                string coeff = Regex.Match(coeffMatches[i].Value, @"[+-]?\d*").Value;

                if (string.IsNullOrWhiteSpace(coeff) || coeff == "+")
                    coeff = "1";
                else if (coeff == "-")
                    coeff = "-1";

                terms.Add($"{coeff}{varMatches[i].Value}");
            }

            string op = line.Contains("<=") ? "<=" :
                        line.Contains(">=") ? ">=" :
                        line.Contains("=") ? "=" : "?";

            string rhs = Regex.Match(line, @"\d+\s*$").Value;

            return string.Join(" ", terms) + " " + op + " " + rhs;
        }

        private double[,] CreateSimplexTableau(List<int> objectiveCoeffs, List<string> constraints)
        {
            int numVariables = objectiveCoeffs.Count;
            int numConstraints = constraints.Count;
            int numSlack = numConstraints;

            int width = numVariables + numSlack + 1; // +1 for RHS
            int height = numConstraints + 1; // +1 for objective row

            double[,] tableau = new double[height, width];

            // Fill objective function in FIRST row (row 0)
            for (int j = 0; j < numVariables; j++)
            {
                tableau[0, j] = -objectiveCoeffs[j]; // maximization → negative
            }

            // Fill constraints from row 1 down
            for (int i = 0; i < numConstraints; i++)
            {
                string constraint = constraints[i];
                var coeffMatches = Regex.Matches(constraint, @"([+-]?\d*)\s*\*?\s*x(\d+)");
                foreach (Match match in coeffMatches)
                {
                    string coeffStr = match.Groups[1].Value.Trim();
                    string varIndexStr = match.Groups[2].Value;

                    int varIndex = int.Parse(varIndexStr) - 1;
                    int coeff = string.IsNullOrEmpty(coeffStr) || coeffStr == "+" ? 1 :
                                coeffStr == "-" ? -1 : int.Parse(coeffStr);

                    tableau[i + 1, varIndex] = coeff;
                }

                // Add slack variable
                tableau[i + 1, numVariables + i] = 1;

                // Add RHS
                var rhsMatch = Regex.Match(constraint, @"(\d+)\s*$");
                if (rhsMatch.Success)
                {
                    tableau[i + 1, width - 1] = double.Parse(rhsMatch.Value);
                }
            }

            return tableau;
        }
        private void DisplayTableau(double[,] tableau)
        {
            txtOutput.AppendText("\r\nSimplex Tableau:\r\n");

            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int numConstraints = rows - 1;
            int numSlack = numConstraints;
            int numVariables = cols - numSlack - 1;

            // ==== Header ====
            txtOutput.AppendText("     "); // Padding for row labels
            for (int j = 0; j < numVariables; j++)
            {
                txtOutput.AppendText($"{("x" + (j + 1)),6}");
            }
            for (int j = 0; j < numSlack; j++)
            {
                txtOutput.AppendText($"{("s" + (j + 1)),6}");
            }
            txtOutput.AppendText($"{"RHS",6}\r\n");

            // ==== Rows ====
            for (int i = 0; i < rows; i++)
            {
                if (i == 0)
                    txtOutput.AppendText("Z     ");
                else
                    txtOutput.AppendText($"C{i}    ");

                for (int j = 0; j < cols; j++)
                {
                    txtOutput.AppendText($"{tableau[i, j],6:F2}");
                }
                txtOutput.AppendText("\r\n");
            }
        }

    }
}
