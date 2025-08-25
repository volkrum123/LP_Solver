using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal sealed class KnapsackParser
    {
        public KnapsackModel Parse(string input)
        {
            var lines = input
                .Replace(",", " ")
                .Replace("\t", " ")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var objLine = lines.FirstOrDefault(l =>
                l.StartsWith("max", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("min", StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException("No objective line found. Expected 'max ...' or 'min ...'.");

            bool isMin = objLine.StartsWith("min", StringComparison.OrdinalIgnoreCase);
            bool hasVarsInObj = Regex.IsMatch(objLine, @"x\s*\d", RegexOptions.IgnoreCase);

            List<int> values;
            if (hasVarsInObj)
            {
                var matches = Regex.Matches(objLine, @"([+-]?\d*\.?\d*)\s*\*?\s*x\s*(\d+)", RegexOptions.IgnoreCase)
                                   .Cast<Match>().ToList();
                if (matches.Count == 0) throw new ArgumentException("Could not parse objective coefficients with variables.");

                int maxIdx = matches.Select(m => int.Parse(m.Groups[2].Value)).Max();
                var vals = new double[maxIdx];
                foreach (var m in matches)
                {
                    var tok = m.Groups[1].Value;
                    double c = string.IsNullOrWhiteSpace(tok) || tok == "+" ? 1
                             : tok == "-" ? -1
                             : double.Parse(tok, CultureInfo.InvariantCulture);
                    int idx = int.Parse(m.Groups[2].Value) - 1;
                    if (idx < 0) throw new ArgumentException("Invalid variable index in objective.");
                    vals[idx] = c;
                }
                values = vals.Select(d => (int)Math.Round(d)).ToList();
            }
            else
            {
                values = Regex.Matches(objLine, @"[+-]?\d*\.?\d+")
                              .Cast<Match>()
                              .Select(m => (int)Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture)))
                              .ToList();
                if (values.Count == 0) throw new ArgumentException("No coefficients found on the objective line.");
            }

            bool converted = false;
            if (isMin) { values = values.Select(v => -v).ToList(); converted = true; }

            int n = values.Count;

            var capLine = lines.FirstOrDefault(l => l.Contains("<="))
                ?? throw new ArgumentException("No '<=' capacity constraint found.");

            var rhsMatch = Regex.Match(capLine, @"<=\s*(-?\d*\.?\d+)\s*$");
            if (!rhsMatch.Success) throw new ArgumentException("No RHS capacity found on the '<=' line.");
            int capacity = (int)Math.Round(double.Parse(rhsMatch.Groups[1].Value, CultureInfo.InvariantCulture));

            int le = capLine.IndexOf("<=");
            if (le < 0) throw new ArgumentException("No '<=' found in capacity line.");
            var lhs = capLine.Substring(0, le);
            bool hasVarsInCap = Regex.IsMatch(lhs, @"x\s*\d", RegexOptions.IgnoreCase);

            List<int> weights;
            if (hasVarsInCap)
            {
                var tmp = new double[n];
                foreach (Match m in Regex.Matches(lhs, @"([+-]?\d*\.?\d*)\s*\*?\s*x\s*(\d+)", RegexOptions.IgnoreCase))
                {
                    var tok = m.Groups[1].Value;
                    double c = string.IsNullOrWhiteSpace(tok) || tok == "+" ? 1
                             : tok == "-" ? -1
                             : double.Parse(tok, CultureInfo.InvariantCulture);

                    int idx = int.Parse(m.Groups[2].Value) - 1;
                    if (idx < 0) throw new ArgumentException("Invalid variable index in capacity row.");
                    if (idx >= tmp.Length)
                    {
                        Array.Resize(ref tmp, idx + 1);
                        values.AddRange(Enumerable.Repeat(0, idx + 1 - n));
                        n = tmp.Length;
                    }
                    tmp[idx] = c;
                }
                weights = tmp.Select(d => (int)Math.Round(d)).ToList();
            }
            else
            {
                weights = Regex.Matches(lhs, @"[+-]?\d*\.?\d+")
                               .Cast<Match>()
                               .Select(m => (int)Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture)))
                               .ToList();
                if (weights.Count == 0) throw new ArgumentException("No weights found on the LHS of '<='.");
            }

            if (weights.Count != n) throw new ArgumentException($"Mismatch: objective has {n} coefficients but capacity has {weights.Count} weights.");
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive.");
            if (weights.Any(w => w <= 0)) throw new ArgumentException("All weights must be positive.");

            var items = Enumerable.Range(0, n)
                .Select(i => new KnapsackItem(i, weights[i], values[i]))
                .ToList();

            return new KnapsackModel(isMaximize: true, wasMinConvertedToMax: converted, capacity: capacity, items: items);
        }
    }
}
