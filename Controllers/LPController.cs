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

            

            // ---- Visuals & summary ----
            RenderKnapsackCanonical(values, weights, capacity, logOutput);

            var trace = new KnapsackTrace();
            var res = KnapsackBBSolver.SolveBacktracking(items, capacity, _ => { }, trace);

            RenderAsciiRatioTable(trace, logOutput);
            RenderBnbIterationTable(trace, logOutput);
            //RenderBnbBlocks(trace, logOutput);

            // ---- Knapsack Result ----

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

        // =============== ASCII TABLE: Ratio Test =================
        private static void RenderAsciiRatioTable(KnapsackTrace trace, Action<string> log)
        {
            var culture = CultureInfo.InvariantCulture;

            var headers = new[] { "Item", "w", "v", "v/w", "rank" };
            var rows = trace.RatioTable
                .Select(r => new[]
                {
            $"x{r.originalIndex + 1}",
            r.weight.ToString(culture),
            r.value.ToString(culture),
            r.ratio.ToString("0.000", culture),
            r.rank.ToString(culture)
                })
                .ToList();

            var widths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                widths[j] = headers[j].Length;
                foreach (var row in rows) widths[j] = Math.Max(widths[j], row[j].Length);
            }

            string Sep(char left, char mid, char right, char fill)
            {
                var parts = widths.Select(w => new string(fill, w + 2));
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

        // =============== Canonical Form =================
        private static void RenderKnapsackCanonical(
            IList<int> values, IList<int> weights, int capacity, Action<string> log)
        {
            string ObjTerms() =>
                string.Join(" + ", Enumerable.Range(0, values.Count).Select(i => $"{values[i]}*x{i + 1}"));

            string CapTerms() =>
                string.Join(" + ", Enumerable.Range(0, weights.Count).Select(i => $"{weights[i]}*x{i + 1}"));

            log("\r\n=== Canonical Form (Knapsack IP) ===\r\n");
            log($"Maximize: z = {ObjTerms()}\r\n");
            log("Subject to:\r\n");
            log($"  {CapTerms()} <= {capacity}\r\n");
            log($"Binary: x1..x{values.Count} ∈ {{0,1}}\r\n");
        }
        private static void RenderBnbIterationTable(KnapsackTrace trace, Action<string> log)
        {
            var culture = CultureInfo.InvariantCulture;

            // Build rows in insertion order (the order you appended TraceNode)
            var rows = new List<string[]>();
            int step = 1;

            foreach (var e in trace.Nodes)
            {
                // Pretty label: P → Sub-p, ".0"→"1", ".1"→"2"
                string Label(string path)
                {
                    if (path == "P") return "Sub-p";
                    var steps = path.Split('.').Skip(1).Select(s => s == "0" ? "1" : "2");
                    return "Sub-p" + string.Join(".", steps);
                }

                string path = Label(e.Path);
                string item =
                    e.ItemOriginalIndex < 0 ? "-" :
                    $"x{e.ItemOriginalIndex + 1}";
                string decision =
                    e.Decision == null ? "-" :
                    (e.Decision == 1 ? "1" : "0");

                rows.Add(new[]
                {
            step.ToString(culture),
            path,
            item,
            decision,
            e.Weight.ToString(culture),
            e.Value.ToString(culture),
            e.Bound.ToString("0.000", culture),
            e.Status,
            string.IsNullOrWhiteSpace(e.Reason) ? "" : e.Reason
        });
                step++;
            }

            // Column headers
            var headers = new[] { "#", "Path", "Item", "Dec", "w", "v", "UB", "Status", "Reason" };

            // Compute widths
            var widths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                widths[j] = headers[j].Length;
            }
            foreach (var r in rows)
            {
                for (int j = 0; j < r.Length; j++)
                    widths[j] = Math.Max(widths[j], r[j].Length);
            }

            string Sep(char left, char mid, char right, char fill)
            {
                var parts = widths.Select(w => new string(fill, w + 2));
                return left + string.Join(mid.ToString(), parts) + right + "\r\n";
            }

            log("\r\n=== Branch & Bound Iterations ===\r\n");
            log(Sep('+', '+', '+', '-'));
            log("| " + string.Join(" | ", headers.Select((h, j) => h.PadRight(widths[j]))) + " |\r\n");
            log(Sep('+', '+', '+', '-'));
            foreach (var r in rows)
                log("| " + string.Join(" | ", r.Select((c, j) => c.PadRight(widths[j]))) + " |\r\n");
            log(Sep('+', '+', '+', '-'));
        }
        public void SolveNonlinearFromInput(string input, Action<string> log)
        {
            // Accept examples:
            //   max f(theta) = 4*sin(theta)*(1+cos(theta))
            //   interval [0, pi/2]
            //   tol = 0.05
            //   iters = 2
            //
            //   min x^2 on [-5,5], tol=1e-6

            string txt = input.Replace("\r", " ").Replace("\n", " ").Trim();

            bool isMax = Regex.IsMatch(txt, @"\bmax\b", RegexOptions.IgnoreCase);

            // ---- extract f(<var>) = <expr>  OR  fallback after "min|max"
            // captures the variable name if present (x, theta, α, ...), then the rhs expr.
            var mFx = Regex.Match(
                txt,
                @"f\s*\(\s*([^\)]+)\s*\)\s*=\s*(.+?)(?=$|\bon\b|\binterval\b|\[|\btol\b|\biters\b)",
                RegexOptions.IgnoreCase);
            string fexpr;
            if (mFx.Success)
            {
                fexpr = mFx.Groups[2].Value.Trim();
            }
            else
            {
                var mBare = Regex.Match(
                    txt, @"(?:min|max)\s+(.+?)\s+(?:on|interval|\[|tol=|iters=)",
                    RegexOptions.IgnoreCase);
                if (!mBare.Success) throw new ArgumentException("Could not find f(x) expression.");
                fexpr = mBare.Groups[1].Value.Trim();
            }

            // ---- interval [a,b]  (a and b may be expressions like pi/2, 3*pi/4, ...)
            var mIv = Regex.Match(txt, @"\[\s*([^,\]]+)\s*,\s*([^,\]]+)\s*\]");
            if (!mIv.Success) throw new ArgumentException("Missing interval [a,b].");
            double a = ParseScalar(mIv.Groups[1].Value);
            double b = ParseScalar(mIv.Groups[2].Value);
            if (a >= b) throw new ArgumentException("Interval must satisfy a < b.");

            // ---- tolerance (optional) ----
            var mTol = Regex.Match(txt, @"tol\s*=\s*([eE0-9\.\-+]+)");
            double tol = mTol.Success
                ? double.Parse(mTol.Groups[1].Value, CultureInfo.InvariantCulture)
                : 1e-6;

            // ---- iteration cap (optional) ----
            var mIt = Regex.Match(txt, @"iters?\s*=\s*(\d+)");
            int maxIter = mIt.Success ? int.Parse(mIt.Groups[1].Value) : 0; // 0 = auto from tol

            // ---- normalize expression: map theta/α/etc -> x, π/pi -> numeric
            string normExpr = NormalizeExpr(fexpr);

            // ---- compile function (now in terms of x, with sin/cos supported)
            var f0 = ExpressionParser.Compile(normExpr);

            // ---- canonical display (keep user’s original expression for readability)
            log("\r\n=== Canonical Form (Nonlinear, 1D) ===\r\n");
            log($"{(isMax ? "Maximize" : "Minimize")}: f(x) = {fexpr}\r\n");
            log($"Subject to: x ∈ [{a:0.000}, {b:0.000}], tol = {tol:0.000}\r\n");

            // ---- solve (max via Minimize(-f))
            double xstar, fstar; int iters;
            if (isMax)
            {
                (xstar, fstar, iters) = GoldenSectionSolver.Maximize(f0, a, b, tol, maxIter, log);
            }
            else
            {
                (xstar, fstar, iters) = GoldenSectionSolver.Minimize(f0, a, b, tol, maxIter, log);
            }

            // ---- summary ----
            log("\r\n=== Nonlinear Result ===\r\n");
            log($"x* = {xstar:0.000000}\r\n");
            log($"f(x*) = {fstar:0.000000}\r\n");
            log($"Iterations = {iters}\r\n");

            // ---- helpers (local) ----
            static string NormalizeExpr(string s)
            {
                // word-boundary replace variable aliases -> x
                s = Regex.Replace(s, @"\b(theta|θ|alpha|α)\b", "x", RegexOptions.IgnoreCase);

                // replace π/pi with numeric value
                s = Regex.Replace(s, @"\bpi\b", Math.PI.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
                s = s.Replace("π", Math.PI.ToString(CultureInfo.InvariantCulture));

                return s;
            }

            static double ParseScalar(string raw)
            {
                // allow things like "pi/2", "3*pi/4", "1.2"
                string expr = NormalizeExpr(raw);
                var f = ExpressionParser.Compile(expr); // no 'x' in scalar — evaluated at x=0
                return f(0.0);
            }
        }

    }
}
