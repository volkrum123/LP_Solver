using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LP_Solver.Models
{
    internal sealed class NonlinearModel
    {
        public string Expr { get; }
        public Func<double, double> F { get; }
        public double A { get; }
        public double B { get; }
        public double Tol { get; }
        public int MaxIter { get; }
        public bool IsMax { get; }

        public NonlinearModel(string expr, Func<double, double> f, double a, double b,
                              double tol, int maxIter, bool isMax)
        { Expr = expr; F = f; A = a; B = b; Tol = tol; MaxIter = maxIter; IsMax = isMax; }
    }

    internal sealed class NonlinearParser
    {
        public NonlinearModel Parse(string input)
        {
            var fLine = Regex.Match(input, @"f\s*\(\s*x\s*\)\s*=\s*(.+)", RegexOptions.IgnoreCase);
            var rng = Regex.Match(input, @"\[\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*\]");
            if (!fLine.Success || !rng.Success)
                throw new ArgumentException("Provide lines like:  f(x) = ...   and an interval  [a,b]");

            string expr = fLine.Groups[1].Value.Trim();
            double a = double.Parse(rng.Groups[1].Value, CultureInfo.InvariantCulture);
            double b = double.Parse(rng.Groups[2].Value, CultureInfo.InvariantCulture);
            if (a > b) { var t = a; a = b; b = t; }

            bool isMax = Regex.IsMatch(input, @"\bmax\b", RegexOptions.IgnoreCase);

            var mt = Regex.Match(input, @"\b(?:tol|eps)\s*=\s*([0-9]+(?:\.[0-9]+)?(?:e-?[0-9]+)?)", RegexOptions.IgnoreCase);
            double tol = mt.Success ? double.Parse(mt.Groups[1].Value, CultureInfo.InvariantCulture) : 1e-6;

            var mi = Regex.Match(input, @"\bmaxiter\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            int maxIter = mi.Success ? int.Parse(mi.Groups[1].Value, CultureInfo.InvariantCulture) : 0;

            // math expression -> delegate
            var f = ExpressionParser.Compile(expr);

            return new NonlinearModel(expr, f, a, b, tol, maxIter, isMax);
        }
    }
}

