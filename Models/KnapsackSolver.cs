using System;
using System.Collections.Generic;
using System.Linq;
using LP_Solver.Models;

namespace LP_Solver.Solvers
{
    internal static class KnapsackBBSolver
    {
        private sealed class Node
        {
            public int Level;
            public int Weight;
            public int Value;
            public double Bound;
            public bool[] Picks;
            public string Path; // for trace

            public Node(int n) { Picks = new bool[n]; }
            public Node(Node other)
            {
                Level = other.Level;
                Weight = other.Weight;
                Value = other.Value;
                Bound = other.Bound;
                Picks = (bool[])other.Picks.Clone();
                Path = other.Path;
            }
        }

        private sealed class MaxPQ<T>
        {
            private readonly List<T> _heap = new List<T>();
            private readonly Comparison<T> _cmp;
            public int Count => _heap.Count;
            public MaxPQ(Comparison<T> cmp) => _cmp = cmp;
            public void Push(T x) { _heap.Add(x); SiftUp(_heap.Count - 1); }
            public T Pop()
            {
                var top = _heap[0];
                var last = _heap[_heap.Count - 1];
                _heap[0] = last;
                _heap.RemoveAt(_heap.Count - 1);
                if (_heap.Count > 0) SiftDown(0);
                return top;
            }
            private void SiftUp(int i)
            {
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (_cmp(_heap[p], _heap[i]) >= 0) break;
                    (_heap[p], _heap[i]) = (_heap[i], _heap[p]);
                    i = p;
                }
            }
            private void SiftDown(int i)
            {
                int n = _heap.Count;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, best = i;
                    if (l < n && _cmp(_heap[l], _heap[best]) > 0) best = l;
                    if (r < n && _cmp(_heap[r], _heap[best]) > 0) best = r;
                    if (best == i) break;
                    (_heap[i], _heap[best]) = (_heap[best], _heap[i]);
                    i = best;
                }
            }
        }

        public static KnapsackResult Solve(
    IList<KnapsackItem> items,
    int capacity,
    Action<string> log = null,
    KnapsackTrace trace = null)
        {
            var result = new KnapsackResult { Capacity = capacity };
            if (items == null || items.Count == 0 || capacity <= 0) return result;

            // Sort by value/weight desc
            var sorted = items.OrderByDescending(it => it.Ratio).ToList();
            int n = sorted.Count;

            // Trace: ratio table with rank
            if (trace != null)
            {
                var ranked = sorted
                    .Select((it, i) => (it.Index, it.Weight, it.Value, it.Ratio, rank: 0))
                    .OrderByDescending(x => x.Ratio)
                    .ToList();
                for (int i = 0; i < ranked.Count; i++)
                    trace.RatioTable.Add((ranked[i].Index, ranked[i].Weight, ranked[i].Value, ranked[i].Ratio, i + 1));
            }

            int bestValue = 0, bestWeight = 0;
            bool[] bestPickSorted = new bool[n];

            var pq = new MaxPQ<Node>((a, b) => a.Bound.CompareTo(b.Bound));
            int explored = 0, pruned = 0;

            double Bound(int level, int w, int v)
            {
                if (w > capacity) return 0.0;
                double bound = v;
                int totalW = w;
                for (int i = level; i < n; i++)
                {
                    var it = sorted[i];
                    if (totalW + it.Weight <= capacity)
                    {
                        totalW += it.Weight;
                        bound += it.Value;
                    }
                    else
                    {
                        int remain = capacity - totalW;
                        if (remain > 0) bound += it.Ratio * remain;
                        break;
                    }
                }
                return bound;
            }

            var root = new Node(n) { Level = 0, Weight = 0, Value = 0, Path = "P" };
            root.Bound = Bound(0, 0, 0);
            pq.Push(root);
            log?.Invoke($"[B&B] Start: capacity={capacity}, items={n}\r\n");
            trace?.Nodes.Add(new TraceNode
            {
                Path = root.Path,
                Level = 0,
                Decision = null,
                ItemSortedIndex = -1,
                ItemOriginalIndex = -1,
                Weight = 0,
                Value = 0,
                Bound = root.Bound,
                Status = "Expand",
                Reason = ""
            });

            while (pq.Count > 0)
            {
                var node = pq.Pop();
                explored++;

                if (node.Bound <= bestValue)
                {
                    pruned++;
                    trace?.Nodes.Add(new TraceNode
                    {
                        Path = node.Path,
                        Level = node.Level,
                        Decision = null,
                        ItemSortedIndex = node.Level - 1,
                        ItemOriginalIndex = node.Level - 1 >= 0 ? sorted[node.Level - 1].Index : -1,
                        Weight = node.Weight,
                        Value = node.Value,
                        Bound = node.Bound,
                        Status = "Prune",
                        Reason = "bound<=incumbent"
                    });
                    continue;
                }
                if (node.Level >= n) continue;

                var item = sorted[node.Level];

                // ---------------- EXCLUDE branch FIRST (decision = 0) ----------------
                var without = new Node(node)
                {
                    Level = node.Level + 1,
                    Path = node.Path + ".0"
                };
                without.Picks[node.Level] = false;
                without.Bound = Bound(without.Level, node.Weight, node.Value);

                if (without.Bound > bestValue)
                {
                    pq.Push(without);
                    trace?.Nodes.Add(new TraceNode
                    {
                        Path = without.Path,
                        Level = without.Level,
                        Decision = 0,
                        ItemSortedIndex = node.Level,
                        ItemOriginalIndex = item.Index,
                        Weight = node.Weight,
                        Value = node.Value,
                        Bound = without.Bound,
                        Status = "Push",
                        Reason = ""
                    });
                }
                else
                {
                    pruned++;
                    trace?.Nodes.Add(new TraceNode
                    {
                        Path = without.Path,
                        Level = without.Level,
                        Decision = 0,
                        ItemSortedIndex = node.Level,
                        ItemOriginalIndex = item.Index,
                        Weight = node.Weight,
                        Value = node.Value,
                        Bound = without.Bound,
                        Status = "Prune",
                        Reason = "bound<=incumbent"
                    });
                }

                // ---------------- INCLUDE branch SECOND (decision = 1) ---------------
                var with = new Node(node)
                {
                    Level = node.Level + 1,
                    Weight = node.Weight + item.Weight,
                    Value = node.Value + item.Value,
                    Path = node.Path + ".1"
                };
                with.Picks[node.Level] = true;

                if (with.Weight <= capacity)
                {
                    if (with.Value > bestValue)
                    {
                        bestValue = with.Value;
                        bestWeight = with.Weight;
                        bestPickSorted = (bool[])with.Picks.Clone();

                        log?.Invoke($"  * Incumbent update @ {with.Path}: value={bestValue}, weight={bestWeight}\r\n");
                        trace?.Nodes.Add(new TraceNode
                        {
                            Path = with.Path,
                            Level = with.Level,
                            Decision = 1,
                            ItemSortedIndex = node.Level,
                            ItemOriginalIndex = item.Index,
                            Weight = with.Weight,
                            Value = with.Value,
                            Bound = 0,
                            Status = "Incumbent",
                            Reason = ""
                        });
                    }

                    with.Bound = Bound(with.Level, with.Weight, with.Value);
                    if (with.Bound > bestValue)
                    {
                        pq.Push(with);
                        trace?.Nodes.Add(new TraceNode
                        {
                            Path = with.Path,
                            Level = with.Level,
                            Decision = 1,
                            ItemSortedIndex = node.Level,
                            ItemOriginalIndex = item.Index,
                            Weight = with.Weight,
                            Value = with.Value,
                            Bound = with.Bound,
                            Status = "Push",
                            Reason = ""
                        });
                    }
                    else
                    {
                        pruned++;
                        trace?.Nodes.Add(new TraceNode
                        {
                            Path = with.Path,
                            Level = with.Level,
                            Decision = 1,
                            ItemSortedIndex = node.Level,
                            ItemOriginalIndex = item.Index,
                            Weight = with.Weight,
                            Value = with.Value,
                            Bound = with.Bound,
                            Status = "Prune",
                            Reason = "bound<=incumbent"
                        });
                    }
                }
                else
                {
                    pruned++;
                    trace?.Nodes.Add(new TraceNode
                    {
                        Path = with.Path,
                        Level = with.Level,
                        Decision = 1,
                        ItemSortedIndex = node.Level,
                        ItemOriginalIndex = item.Index,
                        Weight = with.Weight,
                        Value = with.Value,
                        Bound = 0,
                        Status = "Prune",
                        Reason = "infeasible"
                    });
                }
            } // <-- close while loop here

            // ---------------------- Build final result (after loop) -------------------
            var sortedToOriginal = new int[n];
            for (int si = 0; si < n; si++) sortedToOriginal[si] = sorted[si].Index;

            var decisionOriginal = new bool[items.Count];
            for (int si = 0; si < n; si++)
                decisionOriginal[sortedToOriginal[si]] = bestPickSorted[si];

            var taken = new List<KnapsackItem>();
            for (int i = 0; i < items.Count; i++) if (decisionOriginal[i]) taken.Add(items[i]);

            result.BestValue = bestValue;
            result.BestWeight = bestWeight;
            result.DecisionVector = decisionOriginal;
            result.ItemsTaken = taken;
            result.NodesExplored = explored;
            result.NodesPruned = pruned;

            log?.Invoke($"\r\n[B&B] Done. BestValue={bestValue}, BestWeight={bestWeight}, " +
                        $"Explored={explored}, Pruned={pruned}\r\n");
            return result;
        }

    }
}
