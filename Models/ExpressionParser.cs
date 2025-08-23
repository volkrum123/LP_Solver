using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace LP_Solver.Models
{
    // Minimal expression evaluator for: numbers (incl. scientific notation), x, + - * / ^ and parentheses
    // Examples: "x^2", "x^2 + 3*x + 2", "-(x-1)^2 + 4", "1e-6*x"
    internal static class ExpressionParser
    {
        private enum TokType { Num, VarX, Op, LPar, RPar }

        private struct Tok
        {
            public TokType T;
            public string S;
            public double V;
            public Tok(TokType t, string s, double v) { T = t; S = s; V = v; }
        }

        public static Func<double, double> Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("Empty function expression.");

            var toks = Tokenize(expr);
            var rpn = ToRpn(toks);
            return x => EvalRpn(rpn, x);
        }

        // ---------------- Tokenize ----------------

        private static List<Tok> Tokenize(string s)
        {
            var list = new List<Tok>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                // number (supports scientific notation)
                if (char.IsDigit(c) || c == '.')
                {
                    if (!TryReadNumber(s, ref i, out double num))
                        throw new ArgumentException($"Invalid number near position {i}.");
                    list.Add(new Tok(TokType.Num, "", num));
                    continue;
                }

                // variable x (case-insensitive)
                if (char.ToLowerInvariant(c) == 'x')
                {
                    list.Add(new Tok(TokType.VarX, "x", 0));
                    i++;
                    continue;
                }

                // operators
                if ("+-*/^".IndexOf(c) >= 0)
                {
                    // unary minus => inject 0 before '-'
                    bool needZero = (list.Count == 0) ||
                                    list[list.Count - 1].T == TokType.Op ||
                                    list[list.Count - 1].T == TokType.LPar;

                    if (c == '-' && needZero)
                        list.Add(new Tok(TokType.Num, "", 0));

                    // unary plus: just ignore if at the same positions
                    if (c == '+' && needZero)
                    {
                        i++;
                        continue;
                    }

                    list.Add(new Tok(TokType.Op, c.ToString(), 0));
                    i++;
                    continue;
                }

                if (c == '(') { list.Add(new Tok(TokType.LPar, "(", 0)); i++; continue; }
                if (c == ')') { list.Add(new Tok(TokType.RPar, ")", 0)); i++; continue; }

                throw new ArgumentException($"Unsupported character '{c}' at position {i}.");
            }
            return list;
        }

        // Parse a floating-point literal possibly with scientific notation.
        // Advances 'i' to the first char after the number.
        private static bool TryReadNumber(string s, ref int i, out double value)
        {
            int start = i;
            bool seenDot = false, seenExp = false;
            int len = s.Length;

            // main mantissa part
            while (i < len)
            {
                char c = s[i];
                if (char.IsDigit(c)) { i++; continue; }
                if (c == '.' && !seenDot && !seenExp) { seenDot = true; i++; continue; }
                if ((c == 'e' || c == 'E') && !seenExp)
                {
                    seenExp = true; i++;
                    // optional sign after exponent
                    if (i < len && (s[i] == '+' || s[i] == '-')) i++;
                    // must have at least one digit for exponent
                    if (i >= len || !char.IsDigit(s[i])) { value = 0; return false; }
                    // read exponent digits
                    while (i < len && char.IsDigit(s[i])) i++;
                    break;
                }
                break;
            }

            var token = s.Substring(start, i - start);
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        // ---------------- Infix -> RPN (Shunting-yard) ----------------

        private static int Prec(string op)
        {
            if (op == "^") return 4;
            if (op == "*" || op == "/") return 3;
            if (op == "+" || op == "-") return 2;
            return 0;
        }

        private static bool RightAssoc(string op) { return op == "^"; }

        private static List<Tok> ToRpn(List<Tok> toks)
        {
            var output = new List<Tok>();
            var ops = new Stack<Tok>();

            foreach (var t in toks)
            {
                if (t.T == TokType.Num || t.T == TokType.VarX)
                {
                    output.Add(t);
                }
                else if (t.T == TokType.Op)
                {
                    while (ops.Count > 0 && ops.Peek().T == TokType.Op &&
                           (Prec(ops.Peek().S) > Prec(t.S) ||
                            (Prec(ops.Peek().S) == Prec(t.S) && !RightAssoc(t.S))))
                    {
                        output.Add(ops.Pop());
                    }
                    ops.Push(t);
                }
                else if (t.T == TokType.LPar) ops.Push(t);
                else if (t.T == TokType.RPar)
                {
                    while (ops.Count > 0 && ops.Peek().T != TokType.LPar) output.Add(ops.Pop());
                    if (ops.Count == 0) throw new ArgumentException("Mismatched parentheses.");
                    ops.Pop(); // pop '('
                }
            }

            while (ops.Count > 0)
            {
                if (ops.Peek().T == TokType.LPar) throw new ArgumentException("Mismatched parentheses.");
                output.Add(ops.Pop());
            }
            return output;
        }

        // ---------------- Evaluate RPN ----------------

        private static double EvalRpn(List<Tok> rpn, double x)
        {
            var st = new Stack<double>();
            foreach (var t in rpn)
            {
                if (t.T == TokType.Num) st.Push(t.V);
                else if (t.T == TokType.VarX) st.Push(x);
                else // operator
                {
                    if (st.Count < 2) throw new ArgumentException("Malformed expression.");
                    double b = st.Pop(), a = st.Pop();
                    switch (t.S)
                    {
                        case "+": st.Push(a + b); break;
                        case "-": st.Push(a - b); break;
                        case "*": st.Push(a * b); break;
                        case "/": st.Push(a / b); break;
                        case "^": st.Push(Math.Pow(a, b)); break;
                        default: throw new ArgumentException("Unknown operator " + t.S);
                    }
                }
            }
            if (st.Count != 1) throw new ArgumentException("Malformed expression (stack).");
            return st.Pop();
        }
    }
}


