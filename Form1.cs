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

    }
}
