using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal class LPParser
    {
        public LPModel Parse(string input)
        {
            var model = new LPModel();
            string[] lines = input
                .Replace(",", "\n")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim().ToLower();

                if (line.StartsWith("max"))
                {
                    model.ObjectiveType = "Max";
                }
                else if (line.StartsWith("min"))
                {
                    model.ObjectiveType = "Min";
                }

                if(line.Contains("z") && line.Contains("="))
                {
                    model.ObjectiveCoefficients = GetObjectiveCoefficients(line);
                }
                else if(line.Contains("<=") || line.Contains(">=") || line.Contains("="))
                {
                    model.Constraints.Add(ParseConstraint(line));
                }
            }
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim().ToLower();

                if (line.Contains("integer"))
                {
                    var matches = Regex.Matches(line, @"x(\d+)");
                    if (matches.Count > 0)
                    {
                        foreach (Match m in matches)
                        {
                            int idx = int.Parse(m.Groups[1].Value) - 1;
                            if (!model.IntegerIndices.Contains(idx))
                                model.IntegerIndices.Add(idx);
                        }
                    }
                    else if (line.Contains("all"))
                    {
                        for (int i = 0; i < model.ObjectiveCoefficients.Count; i++)
                            if (!model.IntegerIndices.Contains(i))
                                model.IntegerIndices.Add(i);
                    }
                }
            }
            return model;   
        }

        private List<double> GetObjectiveCoefficients( string line)
        {
            var matches = Regex.Matches(line, @"([+-]?\d*\.?\d*)\s*\*?\s*x\d+");
            return matches.Cast<Match>().Select(m =>
            {
                string coeff = Regex.Match(m.Value, @"[+-]?\d*\.?\d*").Value;
                if(string.IsNullOrWhiteSpace(coeff) || coeff == "+")
                {
                    return 1.0;
                }
                if (coeff == "-")
                {
                    return -1.0;
                }
                return double.Parse(coeff);
            }).ToList();
        }

        private string ParseConstraint(string line )
        {
            var coeffMatches = Regex.Matches(line, @"([+-]?\d*\.?\d*)\s*\*?\s*x\d+");
            var varMatches = Regex.Matches(line, @"x\d+");

            List<string> terms = new List<string>();

            for(int i = 0; i < varMatches.Count; i++)
            {
                string coeff = Regex.Match(coeffMatches[i].Value, @"[+-]?\d*\.?\d*").Value;
                if (string.IsNullOrWhiteSpace(coeff) || coeff == "+")
                {
                    coeff = "1";
                }
                else if(coeff == "-")
                {
                    coeff = "-1";
                }
                terms.Add($"{coeff}{varMatches[i].Value}");
            }
            string op = line.Contains("<=") ? "<=" :
                        line.Contains(">=") ? ">=" :
                        line.Contains("=") ? "=" : "?";

            string rhs = Regex.Match(line, @"-?\d*\.?\d+\s*$").Value;
            string c = string.Join(" ", terms)+ " " +op+ " " + rhs;
            return c;
        }
    }
}
