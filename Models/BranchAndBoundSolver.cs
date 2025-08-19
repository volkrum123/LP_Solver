using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class BranchAndBoundSolver
    {
        private readonly SimplexSolver _solver = new SimplexSolver();
        private readonly CanonicalForm _canonicalForm = new CanonicalForm();
        private const double TOL = 1e-6;

        private class Node
        {
            public LPModel Model;
            public string Path;   // e.g., x2<=3 -> -x2<=-4 -> ...
            public int Depth;
        }

        public void SolveBranchAndBound(LPModel root, Action<string> log)
        {
            // If no integer set specified, default to all decision variables
            var integerSet = (root.IntegerIndices != null && root.IntegerIndices.Count > 0)
                ? new HashSet<int>(root.IntegerIndices)
                : new HashSet<int>(Enumerable.Range(0, root.NumVariables));

            log("===== Branch & Bound (Simplex) =====\r\n");
            LogCanonicalForm(root, log, header: "Root Canonical Form");

            double incumbentValue = root.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase)
                ? double.PositiveInfinity : double.NegativeInfinity;
            List<double> incumbentX = null;

            // Backtracking (DFS): use a stack
            var stack = new Stack<Node>();
            stack.Push(new Node { Model = CloneModel(root), Path = "(root)", Depth = 0 });

            int nodeId = 0;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                nodeId++;
                log($"\r\n----- Node #{nodeId} Depth {node.Depth} Path {node.Path} -----\r\n");

                // For primal simplex compatibility, canonicalize >= branch constraints to <= form
                var simplexReady = CanonicalizeForSimplex(node.Model);

                // Show canonical form for this node
                LogCanonicalForm(simplexReady, log, header: $"Node #{nodeId} Canonical Form");

                // Solve LP relaxation at this node (display ALL iterations)
                var (tableau,constraintTypes) = _solver.CreateTableau(simplexReady);
                double[,] OptimalTable = _solver.Solve(tableau,constraintTypes,log,simplexReady.NumVariables, simplexReady.Constraints.Count, simplexReady.ObjectiveType);

                // Extract objective value and solution
                double nodeObj = tableau[0, tableau.GetLength(1) - 1];
                var x = ExtractPrimalSolution(tableau, simplexReady.NumVariables);

                // Fathoming (pruning) rules:

                // 1) Infeasible solution check (rough heuristic): any basic var RHS < -TOL?
                //    (Your solver will throw on unbounded; infeasible might show negatives in RHS)
                bool infeasible = false;
                for (int i = 1; i < tableau.GetLength(0); i++)
                    if (tableau[i, tableau.GetLength(1) - 1] < -TOL) { infeasible = true; break; }
                if (infeasible)
                {
                    log("Infeasible relaxation.\r\n");
                    continue;
                }

                // 2) Bound (objective) pruning
                if (IsWorseThanIncumbent(simplexReady.ObjectiveType, nodeObj, incumbentValue))
                {
                    log($"Bound prune (node z={nodeObj:0.###} vs incumbent {incumbentValue:0.###}).\r\n");
                    continue;
                }

                // 3) Integrality check
                int fracIdx = FindFractionalIndex(x, integerSet);
                if (fracIdx == -1)
                {
                    // integral solution -> update incumbent if better
                    if (IsBetter(simplexReady.ObjectiveType, nodeObj, incumbentValue))
                    {
                        incumbentValue = nodeObj;
                        incumbentX = x;
                        log($"New best integer solution: x = [{string.Join(", ", incumbentX.Select(v => v.ToString("0.###")))}], z = {incumbentValue:0.###}\r\n");
                    }
                    else
                    {
                        log("Integer solution not better than incumbent.\r\n");
                    }
                    // fathomed (no further branching)
                    continue;
                }

                // 4) Branch on a fractional integer var x_k
                double val = x[fracIdx];
                int floorVal = (int)Math.Floor(val);
                int ceilVal = (int)Math.Ceiling(val);

                log($"Branching on x{fracIdx + 1} = {val:0.###} => Left: x{fracIdx + 1} <= {floorVal}, Right: x{fracIdx + 1} >= {ceilVal}\r\n");

                // LEFT child: x_k <= floor(val)  (already <= form)
                var left = CloneModel(simplexReady);
                left.Constraints.Add($"1x{fracIdx + 1} <= {floorVal}");

                // RIGHT child: x_k >= ceil(val) -> convert for primal simplex:
                //   x_k >= c  ⇔  -x_k <= -c
                var right = CloneModel(simplexReady);
                right.Constraints.Add($"-1x{fracIdx + 1} <= {-ceilVal}");

                // Backtracking (DFS): push RIGHT then LEFT so LEFT is processed next
                stack.Push(new Node { Model = right, Depth = node.Depth + 1, Path = $"{node.Path} -> x{fracIdx + 1}≥{ceilVal}" });
                stack.Push(new Node { Model = left, Depth = node.Depth + 1, Path = $"{node.Path} -> x{fracIdx + 1}≤{floorVal}" });
            }

            // Report incumbent
            if (incumbentX != null)
                log($"\r\n===== Best Candidate (Incumbent) =====\r\nx = [{string.Join(", ", incumbentX.Select(v => v.ToString("0.###")))}]\r\nz = {incumbentValue:0.###}\r\n");
            else
                log("\r\n===== No integer-feasible solution found. =====\r\n");
        }

        // ---------- helpers ----------

        private static bool IsBetter(string objType, double cand, double incumbent)
        {
            if (objType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                return cand < incumbent - TOL;
            return cand > incumbent + TOL; // Max
        }

        private static bool IsWorseThanIncumbent(string objType, double cand, double incumbent)
        {
            if (double.IsInfinity(incumbent)) return false; // no incumbent yet
            if (objType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                return cand >= incumbent - TOL;
            return cand <= incumbent + TOL; // Max
        }

        private static int FindFractionalIndex(List<double> x, HashSet<int> integerSet)
        {
            foreach (int i in integerSet)
                if (i < x.Count && Math.Abs(x[i] - Math.Round(x[i])) > TOL)
                    return i;
            return -1;
        }

        private static List<double> ExtractPrimalSolution(double[,] tableau, int numVars)
        {
            var rows = tableau.GetLength(0);
            var cols = tableau.GetLength(1);
            var x = new double[numVars];

            for (int j = 0; j < numVars; j++)
            {
                int pivotRow = -1;
                bool isUnitCol = true;

                for (int i = 1; i < rows; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1) < TOL)
                    {
                        if (pivotRow == -1) pivotRow = i;
                        else { isUnitCol = false; break; }
                    }
                    else if (Math.Abs(tableau[i, j]) > TOL)
                    {
                        isUnitCol = false; break;
                    }
                }

                x[j] = (isUnitCol && pivotRow != -1) ? tableau[pivotRow, cols - 1] : 0.0;
            }
            return x.ToList();
        }

        // Make a copy you can safely mutate
        private static LPModel CloneModel(LPModel m)
        {
            return new LPModel
            {
                ObjectiveType = m.ObjectiveType,
                ObjectiveCoefficients = new List<double>(m.ObjectiveCoefficients),
                Constraints = new List<string>(m.Constraints),
                IntegerIndices = new List<int>(m.IntegerIndices ?? new List<int>())
            };
        }

        // Convert any ">= c" constraints to "-1*xk <= -c" for primal simplex compatibility.
        private static LPModel CanonicalizeForSimplex(LPModel model)
        {
            var clone = CloneModel(model);
            var fixedConstraints = new List<string>();

            foreach (var line in clone.Constraints)
            {
                if (line.Contains(">="))
                {
                    // sum(ai xi) >= b  →  sum(-ai xi) <= -b
                    var flipped = FlipInequality(line);
                    fixedConstraints.Add(flipped);
                }
                else
                {
                    fixedConstraints.Add(line);
                }
            }
            clone.Constraints = fixedConstraints;
            return clone;
        }

        private static string FlipInequality(string line)
        {
            // line like: "2x1 -1x2 >= 5"
            // flip all coeffs and rhs, and use <=
            var coeffMatches = Regex.Matches(line, @"([+-]?\d*\.?\d*)\s*\*?\s*x\d+");
            var varMatches = Regex.Matches(line, @"x\d+");
            var terms = new List<string>();

            for (int i = 0; i < varMatches.Count; i++)
            {
                string coeffStr = Regex.Match(coeffMatches[i].Value, @"[+-]?\d*\.?\d*").Value;
                if (string.IsNullOrWhiteSpace(coeffStr) || coeffStr == "+") coeffStr = "1";
                else if (coeffStr == "-") coeffStr = "-1";
                double coeff = double.Parse(coeffStr);
                coeff = -coeff;
                terms.Add($"{coeff}x{Regex.Match(varMatches[i].Value, @"\d+").Value}");
            }
            string rhs = Regex.Match(line, @"-?\d*\.?\d+\s*$").Value;
            double rhsVal = double.Parse(rhs);
            rhsVal = -rhsVal;

            return $"{string.Join(" ", terms)} <= {rhsVal}";
        }

        private static void LogCanonicalForm(LPModel m, Action<string> log, string header)
        {
            log($"\r\n=== {header} ===\r\n");
            log($"Objective: {m.ObjectiveType} z = {string.Join(" + ", m.ObjectiveCoefficients.Select((c, i) => $"{c}x{i + 1}"))}\r\n");
            foreach (var c in m.Constraints)
                log($"{c}\r\n");
            log($"(Assumed non-negativity: x_i >= 0)\r\n");
        }
    }
}