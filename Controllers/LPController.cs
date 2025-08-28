using LP_Solver.Models;
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
        private LPParser _parser;
        private SimplexSolver _solver;
        private DuelSimplexSolver _dualSolver;
        private BranchAndBoundSolver _bbSolver;
        private CanonicalForm _canonicalForm;
        private RevisedPrimal _revised;
        private CuttingPlaneSolver _cuttingPlaneSolver;

        public LPController()
        {
            _parser = new LPParser();
            _solver = new SimplexSolver();
            _dualSolver = new DuelSimplexSolver();
            _bbSolver = new BranchAndBoundSolver();
            _canonicalForm = new CanonicalForm();
            _revised = new RevisedPrimal();
            _cuttingPlaneSolver = new CuttingPlaneSolver();
        }

        // ====================== PRIMAL SIMPLEX ======================
        public void SolveFromInput(string input, Action<string> logOutput)
        {
            // Basic model
            var model = _parser.Parse(input);
            LogModelAndStandardform(model, logOutput);
            //Initial Tablue

            var (tableau, ConstraintTypes) = _solver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;
            logOutput("\r\nInitial Tableau:\r\n" +
                 _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

            double[,] OptimalTable = _solver.Solve(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);

        }
        // ====================== REVISED PRIMAL SIMPLEX ======================
        public void RevisedSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);


            double[] solution = _revised.Solve(model, logOutput);

        }

        // ====================== DUAL SIMPLEX ======================
        public void DualSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            LogModelAndStandardform(model, logOutput);

            // Create tableau
            var (tableau, ConstraintTypes) = _dualSolver.CreateTableau(model);
            int numVariables = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            // Print initial tableau
            logOutput("\r\nInitial Tableau:\r\n" +
                _canonicalForm.TableauToString(tableau, numVariables, numConstraints, ConstraintTypes));

            // Solve using dual simplex
            double[,] OptimalTable = _dualSolver.SolveDual(tableau, ConstraintTypes, logOutput, numVariables, numConstraints, model.ObjectiveType);

        }

        // ====================== BRANCH & BOUND ======================
        public void BranchAndBoundSolveFromInput(string input, Action<string> logOutput)
        {
            var model = _parser.Parse(input);
            if (model.IntegerIndices == null || model.IntegerIndices.Count == 0)
            {
                // default all decision variables integer (common in assignments)
                for (int i = 0; i < model.ObjectiveCoefficients.Count; i++) model.IntegerIndices.Add(i);
            }
            _bbSolver.SolveBranchAndBound(model, logOutput);
        }

        // ====================== CUTTING PLANE ======================  
        public void CuttingPlaneSolveFromInput(string input, Action<string> logOutput)  // Keep as internal
        {
            var model = _parser.Parse(input);

            // Ensure IntegerIndices is not null
            if (model.IntegerIndices == null)
            {
                model.IntegerIndices = new List<int>();
            }

            // If no integer variables specified, use all variables as integer
            if (model.IntegerIndices.Count == 0)
            {
                logOutput("No integer variables specified. Using all variables as integer.\n");
                for (int i = 0; i < model.ObjectiveCoefficients.Count; i++)
                    model.IntegerIndices.Add(i);
            }

            var result = _cuttingPlaneSolver.Solve(model, logOutput);

            // You can access the result object for further processing if needed
            if (result.SolutionFound)
            {
                logOutput($"Optimal objective value: {result.ObjectiveValue}\n");
            }
        }
        public void LogModelAndStandardform(LPModel model, Action<string> logOutput)
        {
            logOutput($"Objective: {model.ObjectiveType}\r\n");
            logOutput($"Objective Coeffs: {string.Join(", ", model.ObjectiveCoefficients)}\r\n");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                logOutput($"Constraint {i + 1}: {model.Constraints[i]}\r\n");
            }
            // Variables (bounds, type, integer/binary)
            for (int i = 0; i < model.Variables.Count; i++)
            {
                var v = model.Variables[i];

                // Determine type string
                string typeStr = v.Type switch
                {
                    VariableType.Continuous => "Continuous",
                    VariableType.Integer => "Integer",
                    VariableType.Binary => "Binary",
                    _ => "Unknown"
                };

                // Determine bounds string
                string bounds;
                if (double.IsPositiveInfinity(v.UpperBound))
                    bounds = $"{v.LowerBound} ≤ x{i + 1}";
                else
                    bounds = $"{v.LowerBound} ≤ x{i + 1} ≤ {v.UpperBound}";

                logOutput($"Variable x{i + 1}: Type={typeStr}, Bounds: {bounds}\r\n");
            }
            //Canonical From
            string canonicalForm = _canonicalForm.ConvertToCanonicalFormSequential(model);
            logOutput("\r\n" + canonicalForm + "\r\n");
        }

        // ====================== BRANCH & BOUND KNAPSACK ======================
        static string PrettyPath(string p)
        {
            if (string.IsNullOrEmpty(p) || p == "P") return "";
            var segs = p.Split('.').Skip(1).ToArray();
            // convert ONLY if we actually see a legacy '0'
            if (segs.Any(s => s == "0"))
                segs = segs.Select(s => s == "0" ? "1" : s == "1" ? "2" : s).ToArray();
            return string.Join('.', segs);
        }

        public void SolveKnapsackFromInput(string input, Action<string> log)
        {
            var ks = new KnapsackParser().Parse(input);
            if (ks.WasMinConvertedToMax)
                log("Note: objective was Min — normalized to Max by negating values.\r\n");

            // 1) Initial tableau 
            var (tab, cols, rows) = _canonicalForm.BuildKnapsackXOnlyTableau(ks);
            log("\r\n" + _canonicalForm.TableauToStringCustom(tab, cols, rows, title: "Initial Tableau:"));

            // 2) Canonical text block (requirements ask to show Canonical Form)
            log("\r\n=== Canonical Form (Knapsack IP) ===\r\n");
            log($"Maximize: z = {string.Join(" + ", ks.Items.Select(i => $"{i.Value}*x{i.Index + 1}"))}\r\n");
            log("Subject to:\r\n");
            log($"  {string.Join(" + ", ks.Items.Select(i => $"{i.Weight}*x{i.Index + 1}"))} <= {ks.Capacity}\r\n");
            log($"Binary: x1..x{ks.Items.Count} in {{0,1}}\r\n");

            // 3) Solve with DFS/backtracking B&B and capture the trace
            var trace = new KnapsackTrace();
            var res = KnapsackBBSolver.SolveBacktracking(ks.Items, ks.Capacity, log, trace);

            // 4) For every node (sub-problem) with a 0/1 decision, print its tableau
            var decisionsByPath = new Dictionary<string, List<(int index, int decision)>>();
            decisionsByPath["P"] = new List<(int, int)>();

            string Parent(string path)
            {
                int k = path.LastIndexOf('.');
                return k >= 0 ? path.Substring(0, k) : "P";
            }

            foreach (var n in trace.Nodes)
            {
                if (!n.Decision.HasValue || n.ItemOriginalIndex < 0) continue;

                if (!decisionsByPath.TryGetValue(n.Path, out var list))
                {
                    var parent = Parent(n.Path);
                    decisionsByPath[n.Path] = list = decisionsByPath.ContainsKey(parent)
                        ? new List<(int, int)>(decisionsByPath[parent])
                        : new List<(int, int)>();
                }

                list.Add((n.ItemOriginalIndex, n.Decision.Value));

                var (subTab, subCols, subRows) =
                    _canonicalForm.BuildKnapsackXOnlyTableauWithDecisions(ks, list);

                string title = $"Sub-problem {PrettyPath(n.Path)}  (x{n.ItemOriginalIndex + 1} = {n.Decision.Value})";
                if (!double.IsNaN(n.Bound) && !double.IsInfinity(n.Bound)) title += $"   UB={Math.Round(n.Bound, 3):0.###}";
                if (!string.IsNullOrWhiteSpace(n.Status)) title += $"   [{n.Status}]";

                log(_canonicalForm.TableauToStringCustom(subTab, subCols, subRows, title: title));
            }

            // 5) Summary
            var dv = string.Join(", ", res.DecisionVector.Select(b => b ? "1" : "0"));
            log($"\r\n=== Knapsack Result ===\r\n");
            log($"Capacity: {res.Capacity}\r\n");
            log($"Best Value: {res.BestValue}\r\n");
            log($"Best Weight: {res.BestWeight}\r\n");
            log($"Decision Vector: [{dv}]\r\n");
            log($"Nodes Explored: {res.NodesExplored}, Pruned: {res.NodesPruned}\r\n");
        }


        // ====================== NON-LINEAR (Golden Section) ======================
        public void SolveNonlinearFromInput(string input, Action<string> log)
        {
            log ??= _ => { };

            var m = new NonlinearParser().Parse(input);

            log("\r\n=== Nonlinear (Golden Section) ===\r\n");
            log($"Objective: {(m.IsMax ? "Max" : "Min")}\r\n");
            log($"f(x) = {m.Expr}\r\n");
            log(FormattableString.Invariant(
                $"Interval: [{m.A}, {m.B}], tol={m.Tol}, maxIter={(m.MaxIter > 0 ? m.MaxIter : -1)}\r\n"));

            double xstar, fstar; int iters;

            if (m.IsMax)
            {
                var res = GoldenSectionSolver.Maximize(m.F, m.A, m.B, m.Tol, m.MaxIter, log);
                xstar = res.xstar; fstar = res.fstar; iters = res.iters;
            }
            else
            {
                var res = GoldenSectionSolver.Minimize(m.F, m.A, m.B, m.Tol, m.MaxIter, log);
                xstar = res.xstar; fstar = res.fstar; iters = res.iters;
            }

            log(FormattableString.Invariant(
                $"\r\nSummary: x* = {xstar:0.000}, f* = {fstar:0.000}, iterations = {iters}\r\n"));
        }
    }
}
