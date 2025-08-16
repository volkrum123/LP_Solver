using System;
using System.Collections.Generic;
using System.Linq;
using LP_Solver.Models;
using LP_Solver.Solvers;

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
            ParseKnapsack(input, out var items, out var capacity);

            var list = items.Select((p, i) => new KnapsackItem(i, p.weight, p.value)).ToList();
            var trace = new KnapsackTrace();

            var res = KnapsackBBSolver.Solve(list, capacity, logOutput, trace);

            // Visuals
            RenderAsciiRatioTable(trace, logOutput);
            RenderBnbBlocks(trace, logOutput);

            // Final summary
            var dv = string.Join(", ", res.DecisionVector.Select(b => b ? "1" : "0"));
            logOutput($"\r\n=== Knapsack Result ===\r\n");
            logOutput($"Capacity: {res.Capacity}\r\n");
            logOutput($"Best Value: {res.BestValue}\r\n");
            logOutput($"Best Weight: {res.BestWeight}\r\n");
            logOutput($"Decision Vector: [{dv}]\r\n");
            logOutput($"Nodes Explored: {res.NodesExplored}, Pruned: {res.NodesPruned}\r\n");
            logOutput($"Items taken: " +
                      $"{string.Join(", ",
                          list.Where((x, idx) => res.DecisionVector[idx])
                              .Select(x => $"x{x.Index + 1} (w={x.Weight}, v={x.Value})"))}\r\n");
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
