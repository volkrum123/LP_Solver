using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace LP_Solver.Models
{
    internal static class GoldenSectionSolver
    {
        /// <summary>
        /// Minimize f on [a,b] (assumes unimodal).
        /// If maxIter <= 0, it is computed from (b-a) and tol.
        /// Prints an iteration table with all values rounded to 3 dp (culture-invariant).
        /// </summary>
        public static (double xstar, double fstar, int iters) Minimize(
            Func<double, double> f, double a, double b,
            double tol, int maxIter,
            Action<string> log = null)
        {
            if (a > b) { var tmp = a; a = b; b = tmp; }
            if (tol <= 0) tol = 1e-6;

            // If user didn't specify a cap, compute one from interval & tolerance.
            if (maxIter <= 0)
            {
                const double invPhi = 0.6180339887498949; // 1/phi
                double L0 = Math.Abs(b - a);
                int kNeeded = (int)Math.Ceiling(Math.Log(tol / L0) / Math.Log(invPhi));
                if (kNeeded < 1) kNeeded = 1;
                maxIter = kNeeded + 5; // small safety margin
            }

            // Golden-section constants
            double phi = (1.0 + Math.Sqrt(5.0)) / 2.0; // ~1.618
            double r = 2.0 - phi;                      // ~0.382
            double inv = 1.0 - r;                      // ~0.618

            // Initial interior points
            double c = a + r * (b - a);
            double d = a + inv * (b - a);
            double fc = f(c), fd = f(d);

            log?.Invoke("\r\n=== Golden-Section Iterations ===\r\n");
            log?.Invoke("+----+----------+----------+----------+----------+----------+----------+\r\n");
            log?.Invoke("| k  | a        | b        | c        | d        | f(c)     | f(d)     |\r\n");
            log?.Invoke("+----+----------+----------+----------+----------+----------+----------+\r\n");

            int k = 0;
            void Row()
            {
                log?.Invoke(FormattableString.Invariant(
                    $"| {k,2} | {a,8:0.000} | {b,8:0.000} | {c,8:0.000} | {d,8:0.000} | {fc,8:0.000} | {fd,8:0.000} |\r\n"));
            }
            Row();

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
                Row();
            }

            double xstar = 0.5 * (a + b);
            double fstar = f(xstar);

            log?.Invoke("+----+----------+----------+----------+----------+----------+----------+\r\n");
            // 3 dp here too (and invariant)
            log?.Invoke(FormattableString.Invariant(
                $"Result: x* = {xstar:0.000}, f(x*) = {fstar:0.000}\r\n"));

            return (xstar, fstar, k);
        }

        /// <summary>
        /// Maximize f on [a,b] by minimizing -f (assumes unimodal).
        /// Prints the same iteration table as Minimize but for -f; the final f* is restored.
        /// </summary>
        public static (double xstar, double fstar, int iters) Maximize(
            Func<double, double> f, double a, double b,
            double tol, int maxIter,
            Action<string> log = null)
        {
            log?.Invoke("\r\n[Note] Maximization is performed by minimizing -f(x). " +
                        "Iteration table shows values of -f at c and d.\r\n");

            var (x, negf, k) = Minimize(x0 => -f(x0), a, b, tol, maxIter, log);
            return (x, -negf, k);
        }
    }
}


