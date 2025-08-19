using LP_Solver.Models;
using LP_Solver.Solvers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LP_Solver.Controllers
{
    internal class LPController
    {
        private readonly LPParser _parser;
        private readonly SimplexSolver _solver;
        private readonly DuelSimplexSolver _dualSolver;

        public LPController()
        {
            _parser = new LPParser();
            _solver = new SimplexSolver();
            _dualSolver = new DuelSimplexSolver();
        }

        // ------------------------ PRIMAL SIMPLEX ------------------------
        public void SolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);

            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for (int i = 0; i < model.Constraints.Count; i++)
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");

            var tableau = _solver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            logOutput("\r\nInitial Tableau:\r\n" +
                      _solver.TableauToString(tableau, numVariables, numConstraints));
            _solver.Solve(tableau, model.ObjectiveType, logOutput, numVariables, numConstraints);
        }

        // ------------------------ DUAL SIMPLEX --------------------------
        public void DualSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);

            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for (int i = 0; i < model.Constraints.Count; i++)
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");

            var (tableau, ConstraintTypes) = _dualSolver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            logOutput("\r\nInitial Tableau:\r\n" +
                      _dualSolver.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

            _dualSolver.SolveDual(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);
        }

        // ====================== BRANCH & BOUND KNAPSACK ======================

        /// <summary>
        /// Parses knapsack text and solves via Branch & Bound, then prints a visual trace.
        /// </summary>
        public void SolveKnapsackFromInput(string input, Action<string> logOutput)
        {
            // Normalize lines
            var lines = input
                .Replace(",", " ")
                .Replace("\t", " ")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            // ---- Objective line (must start with max|min) ----
            var objLine = lines.FirstOrDefault(l =>
                l.StartsWith("max", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("min", StringComparison.OrdinalIgnoreCase));
            if (objLine == null)
                throw new ArgumentException("No objective line found. Expected 'max ...' or 'min ...'.");

            bool objIsMin = objLine.StartsWith("min", StringComparison.OrdinalIgnoreCase);
            bool objHasVars = Regex.IsMatch(objLine, @"x\s*\d", RegexOptions.IgnoreCase);

            List<int> values;

            if (objHasVars)
            {
                // Parse coefficients tied to x1..xk: e.g., 2x1 + 3x2 ...
                var matches = Regex.Matches(objLine, @"([+-]?\d*\.?\d*)\s*\*?\s*x\s*(\d+)", RegexOptions.IgnoreCase)
                                   .Cast<Match>()
                                   .ToList();
                if (matches.Count == 0)
                    throw new ArgumentException("Could not parse objective coefficients with variables.");

                int maxIdx = matches.Select(m => int.Parse(m.Groups[2].Value)).Max();
                var vals = new double[maxIdx]; // 0-based for x1..xN

                foreach (var m in matches)
                {
                    var tok = m.Groups[1].Value; // coeff token
                    double c;
                    if (string.IsNullOrWhiteSpace(tok) || tok == "+") c = 1;
                    else if (tok == "-") c = -1;
                    else c = double.Parse(tok, CultureInfo.InvariantCulture);

                    int idx = int.Parse(m.Groups[2].Value) - 1;
                    vals[idx] = c;
                }
                values = vals.Select(d => (int)Math.Round(d)).ToList();
            }
            else
            {
                // Compact numbers-only: max +2 +2 +3 ...
                var nums = Regex.Matches(objLine, @"[+-]?\d*\.?\d+")
                                .Cast<Match>()
                                .Select(m => (int)Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture)))
                                .ToList();
                if (nums.Count == 0)
                    throw new ArgumentException("No coefficients found on the objective line.");
                values = nums;
            }

            if (objIsMin)
            {
                values = values.Select(v => -v).ToList();
                logOutput("Note: objective is Min — converted to Max by negating values.\r\n");
            }

            int n = values.Count;

            // ---- Capacity row (first '<=') ----
            var capLine = lines.FirstOrDefault(l => l.Contains("<="));
            if (capLine == null)
                throw new ArgumentException("No '<=' capacity constraint found.");

            // RHS capacity
            var rhsMatch = Regex.Match(capLine, @"<=\s*(-?\d*\.?\d+)\s*$");
            if (!rhsMatch.Success)
                throw new ArgumentException("No RHS capacity found on the '<=' line.");
            int capacity = (int)Math.Round(double.Parse(rhsMatch.Groups[1].Value, CultureInfo.InvariantCulture));

            // LHS weights: either numbers-only or variable-annotated (like 11x1 + 8x2 ...)
            var lhs = capLine.Substring(0, capLine.IndexOf("<="));
            bool capHasVars = Regex.IsMatch(lhs, @"x\s*\d", RegexOptions.IgnoreCase);

            List<int> weights;
            if (capHasVars)
            {
                var tmp = new double[n];
                foreach (Match m in Regex.Matches(lhs, @"([+-]?\d*\.?\d*)\s*\*?\s*x\s*(\d+)", RegexOptions.IgnoreCase))
                {
                    var tok = m.Groups[1].Value;
                    double c;
                    if (string.IsNullOrWhiteSpace(tok) || tok == "+") c = 1;
                    else if (tok == "-") c = -1;
                    else c = double.Parse(tok, CultureInfo.InvariantCulture);

                    int idx = int.Parse(m.Groups[2].Value) - 1;
                    if (idx < 0) throw new ArgumentException("Invalid variable index in capacity row.");
                    if (idx >= tmp.Length)
                    {
                        // Grow if capacity references a higher xk than objective listed
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
                var nums = Regex.Matches(lhs, @"[+-]?\d*\.?\d+")
                                .Cast<Match>()
                                .Select(m => (int)Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture)))
                                .ToList();
                if (nums.Count == 0)
                    throw new ArgumentException("No weights found on the LHS of the '<=' line.");
                weights = nums;
            }

            // ---- Sanity ----
            if (weights.Count != n)
                throw new ArgumentException($"Mismatch: objective has {n} coefficients but capacity row has {weights.Count} weights.");
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive.");
            if (weights.Any(w => w <= 0)) throw new ArgumentException("All weights must be positive.");

            // ---- Build items & solve ----
            var items = Enumerable.Range(0, n)
                .Select(i => new KnapsackItem(i, weights[i], values[i]))
                .ToList();

            var trace = new KnapsackTrace();
            var res = KnapsackBBSolver.Solve(items, capacity, logOutput, trace);

            // ---- Visuals & summary ----
            RenderAsciiRatioTable(trace, logOutput);
            RenderBnbBlocks(trace, logOutput);

            var dv = string.Join(", ", res.DecisionVector.Select(b => b ? "1" : "0"));
            logOutput($"\r\n=== Knapsack Result ===\r\n");
            logOutput($"Capacity: {res.Capacity}\r\n");
            logOutput($"Best Value: {res.BestValue}\r\n");
            logOutput($"Best Weight: {res.BestWeight}\r\n");
            logOutput($"Decision Vector: [{dv}]\r\n");
            logOutput($"Nodes Explored: {res.NodesExplored}, Pruned: {res.NodesPruned}\r\n");
            logOutput($"Items taken: " + $"{string.Join(", ", items.Where((x, idx) => res.DecisionVector[idx]).Select(x => $"x{x.Index + 1} (w={x.Weight}, v={x.Value})"))}\r\n");
        }


        // For programmatic calls (e.g., grid -> list)
        public KnapsackResult SolveKnapsack(
            IList<(int weight, int value)> items,
            int capacity,
            Action<string> logOutput)
        {
            var list = items.Select((p, i) => new KnapsackItem(i, p.weight, p.value)).ToList();
            return KnapsackBBSolver.Solve(list, capacity, logOutput);
        }

        // ---- helper parser for knapsack text input
        private static void ParseKnapsack(
            string input,
            out List<(int weight, int value)> items,
            out int capacity)
        {
            items = new List<(int, int)>();
            capacity = 0;

            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Knapsack input is empty.");

            // 1) capacity = N
            var capMatch = System.Text.RegularExpressions.Regex.Match(
                input, @"capacity\s*=\s*(-?\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!capMatch.Success)
                throw new ArgumentException("Missing 'capacity = N' line.");
            capacity = int.Parse(capMatch.Groups[1].Value);

            // 2) (w,v) pairs anywhere
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(
                         input, @"\(\s*(-?\d+)\s*[,;]\s*(-?\d+)\s*\)"))
            {
                items.Add((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
            }

            // 3) Bare pairs per line: "w v" or "w,v"
            if (items.Count == 0)
            {
                foreach (var raw in input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var ln = raw.Trim();
                    if (ln.ToLower().StartsWith("capacity")) continue;

                    var mm = System.Text.RegularExpressions.Regex.Match(
                        ln, @"^\s*(-?\d+)\s*[,;\s]\s*(-?\d+)\s*$");
                    if (mm.Success)
                        items.Add((int.Parse(mm.Groups[1].Value), int.Parse(mm.Groups[2].Value)));
                }
            }

            if (items.Count == 0)
                throw new ArgumentException("No item pairs found. Use '(weight,value)' or 'weight value' per line.");
        }

        // =============== ASCII TABLE: Ratio Test =================
        private static void RenderAsciiRatioTable(KnapsackTrace trace, Action<string> log)
        {
            var headers = new[] { "Item", "w", "v", "v/w", "rank" };
            var rows = trace.RatioTable
                .Select(r => new[]
                {
                    $"x{r.originalIndex + 1}",
                    r.weight.ToString(),
                    r.value.ToString(),
                    r.ratio.ToString("0.###"),
                    r.rank.ToString()
                })
                .ToList();

            // compute column widths
            var widths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                widths[j] = headers[j].Length;
                foreach (var row in rows) widths[j] = Math.Max(widths[j], row[j].Length);
            }

            string Sep(char left, char mid, char right, char fill)
            {
                var parts = widths.Select(w => new string(fill, w + 2)); // +2 padding
                return left + string.Join(mid.ToString(), parts) + right + "\r\n";
            }

            log("\r\n=== Ratio Test (value/weight) ===\r\n");
            log(Sep('+', '+', '+', '-'));
            log("| " + string.Join(" | ", headers.Select((h, j) => h.PadRight(widths[j]))) + " |\r\n");
            log(Sep('+', '+', '+', '-'));
            foreach (var row in rows)
                log("| " + string.Join(" | ", row.Select((c, j) => c.PadRight(widths[j]))) + " |\r\n");
            log(Sep('+', '+', '+', '-'));
        }

        // =============== Block-style B&B progress (stacked lines) ===============
        private static void RenderBnbBlocks(KnapsackTrace trace, Action<string> log)
        {
            log("\r\n=== Branch & Bound Progress ===\r\n");

            // Group nodes by Path, preserving first-seen order
            var order = new List<string>();
            var groups = new Dictionary<string, List<TraceNode>>();

            foreach (var n in trace.Nodes)
            {
                if (!groups.TryGetValue(n.Path, out var list))
                {
                    list = new List<TraceNode>();
                    groups[n.Path] = list;
                    order.Add(n.Path);
                }
                list.Add(n);
            }

            string Label(string path)
            {
                if (path == "P") return "Sub-p";
                // map ".0" -> "1" (exclude-first), ".1" -> "2"
                var steps = path.Split('.').Skip(1).Select(s => s == "0" ? "1" : "2");
                return "Sub-p" + string.Join(".", steps);
            }


            foreach (var path in order)
            {
                var entries = groups[path];
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    string indent = (i == 0) ? "" : "  ";
                    string item = e.ItemOriginalIndex >= 0 ? $"x{e.ItemOriginalIndex + 1}" : "-";
                    string dec = e.Decision == null ? "" : (e.Decision == 1 ? "=1" : "=0");
                    string bound = e.Bound.ToString("0.###");
                    string reason = string.IsNullOrWhiteSpace(e.Reason) ? "" : $" ({e.Reason})";

                    log($"{indent}{Label(path)}: {item}{dec}  w={e.Weight}, v={e.Value}, bound={bound}  {e.Status}{reason}\r\n");
                }
            }
        }
    }
}
