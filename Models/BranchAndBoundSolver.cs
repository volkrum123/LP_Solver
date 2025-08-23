using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal class BranchAndBoundSolver
    {
        private readonly DuelSimplexSolver _dualSolver = new DuelSimplexSolver();
        private readonly CanonicalForm _canonicalForm = new CanonicalForm();
        private const double TOL = 1e-6;

        private class Node
        {
            public LPModel Model;
            public string Path;  
            public int Depth;
        }

   
        public void SolveBranchAndBound(LPModel root, Action<string> log)
        {
            // If no integer set specified, default to all decision variables.
            var integerSet = (root.IntegerIndices != null && root.IntegerIndices.Count > 0)
                ? new HashSet<int>(root.IntegerIndices)
                : new HashSet<int>(Enumerable.Range(0, root.NumVariables));

            log("===== Branch & Bound (Dual Simplex) =====\n");
            LogCanonicalForm(root, log, header: "Root Canonical Form");

            double incumbentValue = root.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase)
                ? double.PositiveInfinity
                : double.NegativeInfinity;

            List<double> incumbentX = null;

            // Backtracking (DFS)
            var stack = new Stack<Node>();
            stack.Push(new Node { Model = CloneModel(root), Path = "(root)", Depth = 0 });

            int nodeId = 0;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                nodeId++;

                log($"\n----- Node #{nodeId} Depth {node.Depth} Path {node.Path} -----\n");

                // Show canonical form for this node
                LogCanonicalForm(node.Model, log, header: $"Node #{nodeId} Canonical Form");

                // Dual Simplex
                var (tableau, constraintTypes) = _dualSolver.CreateTableau(node.Model);

                double[,] optimal;
                try
                {
                    optimal = _dualSolver.SolveDual(
                        tableau,
                        constraintTypes,
                        log,
                        node.Model.NumVariables,
                        node.Model.Constraints.Count,
                        node.Model.ObjectiveType
                    );
                }
                catch (Exception ex)
                {
                    log($"Infeasible relaxation (exception): {ex.Message}\n");
                    continue; 
                }

               
                double nodeObj = optimal[0, optimal.GetLength(1) - 1];
                var x = ExtractPrimalSolution(optimal, node.Model.NumVariables);

                bool infeasible = false;
                for (int i = 1; i < optimal.GetLength(0); i++)
                {
                    if (optimal[i, optimal.GetLength(1) - 1] < -1e-8)
                    {
                        infeasible = true;
                        break;
                    }
                }
                if (infeasible)
                {
                    log("Fathomed: Infeasible relaxation.\n");
                    continue;
                }

                // Bound pruning
                if (IsWorseThanIncumbent(node.Model.ObjectiveType, nodeObj, incumbentValue))
                {
                    log($"Fathomed: Bound prune (node z={nodeObj:0.###} vs incumbent {incumbentValue:0.###}).\n");
                    continue;
                }

                int fracIdx = FindFractionalIndex(x, integerSet);
                if (fracIdx == -1)
                {
                    // Integral feasible — update incumbent if better
                    if (IsBetter(node.Model.ObjectiveType, nodeObj, incumbentValue))
                    {
                        incumbentValue = nodeObj;
                        incumbentX = x;
                        log($"New best integer solution: x = [{string.Join(", ", incumbentX.Select(v => v.ToString("0.###")))}], z = {incumbentValue:0.###}\n");
                    }
                    else
                    {
                        log("Integer solution not better than incumbent.\n");
                    }
                    continue; 
                }

                double val = x[fracIdx];
                int floorVal = (int)Math.Floor(val);
                int ceilVal = (int)Math.Ceiling(val);

                log($"Branching on x{fracIdx + 1} = {val:0.###} → Left: x{fracIdx + 1} ≤ {floorVal}, Right: x{fracIdx + 1} ≥ {ceilVal}\n");

                // LEFT child
                var left = CloneModel(node.Model);
                left.Constraints.Add($"1x{fracIdx + 1} <= {floorVal}");

                // RIGHT child
                var right = CloneModel(node.Model);
                right.Constraints.Add($"1x{fracIdx + 1} >= {ceilVal}");

              
                stack.Push(new Node { Model = right, Depth = node.Depth + 1, Path = $"{node.Path} -> x{fracIdx + 1}≥{ceilVal}" });
                stack.Push(new Node { Model = left, Depth = node.Depth + 1, Path = $"{node.Path} -> x{fracIdx + 1}≤{floorVal}" });
            }

            if (incumbentX != null)
            {
                log("\n===== Best Candidate (Incumbent) =====\n");
                log($"x = [{string.Join(", ", incumbentX.Select(v => v.ToString("0.###")))}]\n");
                log($"z = {incumbentValue:0.###}\n");
            }
            else
            {
                log("\n===== No integer-feasible solution found. =====\n");
            }
        }

   

        private static bool IsBetter(string objType, double cand, double incumbent)
        {
            if (objType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                return cand < incumbent - TOL;
            return cand > incumbent + TOL; // Max
        }

        private static bool IsWorseThanIncumbent(string objType, double cand, double incumbent)
        {
            if (double.IsInfinity(incumbent)) return false;
            if (objType.Equals("Min", StringComparison.OrdinalIgnoreCase))
                return cand >= incumbent - TOL;
            return cand <= incumbent + TOL; // Max
        }

        private static int FindFractionalIndex(List<double> x, HashSet<int> integerSet)
        {
            foreach (int i in integerSet)
            {
                if (i < x.Count && Math.Abs(x[i] - Math.Round(x[i])) > TOL)
                    return i;
            }
            return -1;
        }
        //dualsolver only returns the optimal table, to get variables, we extract them 
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
                        isUnitCol = false;
                        break;
                    }
                }

                x[j] = (isUnitCol && pivotRow != -1)
                    ? tableau[pivotRow, cols - 1]
                    : 0.0;
            }
            return x.ToList();
        }

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

        private void LogCanonicalForm(LPModel m, Action<string> log, string header)
        {
            log($"\n=== {header} ===\n");
            log($"Objective: {m.ObjectiveType} z = {string.Join(" + ", m.ObjectiveCoefficients.Select((c, i) => $"{c}x{i + 1}"))}\n");
            foreach (var c in m.Constraints)
                log($"{c}\n");
            log($"(Assumed non-negativity: x_i >= 0)\n");
        }
    }
}
