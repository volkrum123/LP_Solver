using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LP_Solver.Models
{
    internal static class GoldenSectionSolver
    {
        /// <summary>
        /// Minimize f on [a,b] (unimodal). Logs one unified iteration table using CanonicalForm.TableauToStringCustom.
        /// </summary>
        public static (double xstar, double fstar, int iters) Minimize(
            Func<double, double> f, double a, double b,
            double tol, int maxIter,
            Action<string> log = null)
        {
            if (a > b) { var t = a; a = b; b = t; }
            if (tol <= 0) tol = 1e-6;

            // If user didn't specify a cap, compute a sensible one from interval & tolerance
            if (maxIter <= 0)
            {
                const double invPhi = 0.6180339887498949; // 1/phi
                double L0 = Math.Abs(b - a);
                int kNeeded = (int)Math.Ceiling(Math.Log(tol / L0) / Math.Log(invPhi));
                if (kNeeded < 1) kNeeded = 1;
                maxIter = kNeeded + 5;
            }

            // Golden-section constants
            double phi = (1.0 + Math.Sqrt(5.0)) / 2.0; // ~1.618
            double r = 2.0 - phi;                    // ~0.382
            double inv = 1.0 - r;                      // ~0.618

            // Initial interior points
            double c = a + r * (b - a);
            double d = a + inv * (b - a);
            double fc = f(c), fd = f(d);

            // Collect rows for pretty table (k, a, b, c, d, f(c), f(d))
            var rows = new List<(double a, double b, double c, double d, double fc, double fd)>();
            int k = 0;
            rows.Add((a, b, c, d, fc, fd));

            // Main loop
            while ((b - a) > tol && k < maxIter)
            {
                if (fd > fc)      // keep [a, d] for minimization
                {
                    b = d; d = c; fd = fc;
                    c = a + r * (b - a); fc = f(c);
                }
                else              // keep [c, b]
                {
                    a = c; c = d; fc = fd;
                    d = a + inv * (b - a); fd = f(d);
                }
                k++;
                rows.Add((a, b, c, d, fc, fd));
            }

            double xstar = 0.5 * (a + b);
            double fstar = f(xstar);

            // Pretty, consistent table with 3dp using CanonicalForm.TableauToStringCustom
            if (log != null)
            {
                var cf = new CanonicalForm();
                var colHeaders = new[] { "a", "b", "c", "d", "f(c)", "f(d)" };
                var rowHeaders = Enumerable.Range(0, rows.Count).Select(i => $"k{i}").ToArray();

                var T = new double[rows.Count, colHeaders.Length];
                for (int i = 0; i < rows.Count; i++)
                {
                    T[i, 0] = rows[i].a;
                    T[i, 1] = rows[i].b;
                    T[i, 2] = rows[i].c;
                    T[i, 3] = rows[i].d;
                    T[i, 4] = rows[i].fc;
                    T[i, 5] = rows[i].fd;
                }

                log("\r\n" + cf.TableauToStringCustom(
                    T, colHeaders, rowHeaders, title: "Golden-Section Iterations:"));

                log(FormattableString.Invariant(
                    $"\r\nResult: x* = {Math.Round(xstar, 3):0.###}, f(x*) = {Math.Round(fstar, 3):0.###}\r\n"));
            }

            return (xstar, fstar, k);
        }

        /// <summary>
        /// Maximize f on [a,b] by minimizing -f (assumes unimodal).
        /// Prints the same iteration table style; final f* is restored.
        /// </summary>
        public static (double xstar, double fstar, int iters) Maximize(
            Func<double, double> f, double a, double b,
            double tol, int maxIter,
            Action<string> log = null)
        {
            log?.Invoke("\r\n[Note] Maximization is performed by minimizing -f(x).\r\n");
            var (x, negf, it) = Minimize(x0 => -f(x0), a, b, tol, maxIter, log);
            return (x, -negf, it);
        }
    }
}
