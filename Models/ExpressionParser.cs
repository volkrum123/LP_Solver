using System;
using System.Collections.Generic;
using System.Globalization;

namespace LP_Solver.Models
{
    // Minimal evaluator: numbers, x, + - * / ^, parentheses, sin(), cos(), and optional 'pi'
    internal static class ExpressionParser
    {
        private enum TokType { Num, VarX, Op, LPar, RPar, Func }

        private struct Tok
        {
            public TokType T; public string S; public double V;
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

        // -------- Tokenize --------
        private static List<Tok> Tokenize(string s)
        {
            var list = new List<Tok>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                // number
                if (char.IsDigit(c) || c == '.')
                {
                    int j = i + 1;
                    while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                    double num = double.Parse(s.Substring(i, j - i), CultureInfo.InvariantCulture);
                    list.Add(new Tok(TokType.Num, "", num));
                    i = j; continue;
                }

                // identifier: x, sin, cos, pi, π
                if (char.IsLetter(c) || c == 'π')
                {
                    int j = i + 1;
                    while (j < s.Length && (char.IsLetter(s[j]) || s[j] == 'π')) j++;
                    string id = s.Substring(i, j - i);
                    string idl = id.ToLowerInvariant();

                    if (idl == "x")
                        list.Add(new Tok(TokType.VarX, "x", 0));
                    else if (idl == "sin" || idl == "cos")
                        list.Add(new Tok(TokType.Func, idl, 0));
                    else if (idl == "pi" || id == "π")
                        list.Add(new Tok(TokType.Num, "", Math.PI));
                    else
                        throw new ArgumentException($"Unknown identifier '{id}'. Use x / sin / cos / pi.");
                    i = j; continue;
                }

                // operators
                if ("+-*/^".IndexOf(c) >= 0)
                {
                    bool needZero =
                        (list.Count == 0) ||
                        list[list.Count - 1].T == TokType.Op ||
                        list[list.Count - 1].T == TokType.LPar ||
                        list[list.Count - 1].T == TokType.Func; // e.g., sin(-x)

                    if (c == '-' && needZero)
                        list.Add(new Tok(TokType.Num, "", 0));

                    list.Add(new Tok(TokType.Op, c.ToString(), 0));
                    i++; continue;
                }

                if (c == '(') { list.Add(new Tok(TokType.LPar, "(", 0)); i++; continue; }
                if (c == ')') { list.Add(new Tok(TokType.RPar, ")", 0)); i++; continue; }

                throw new ArgumentException($"Unsupported character '{c}'.");
            }
            return list;
        }

        // -------- Shunting-yard (infix -> RPN) --------
        private static int Prec(string op)
        {
            if (op == "^") return 4;
            if (op == "*" || op == "/") return 3;
            if (op == "+" || op == "-") return 2;
            return 0;
        }
        private static bool RightAssoc(string op) => op == "^";

        private static List<Tok> ToRpn(List<Tok> toks)
        {
            var output = new List<Tok>();
            var ops = new Stack<Tok>();

            foreach (var t in toks)
            {
                if (t.T == TokType.Num || t.T == TokType.VarX)
                    output.Add(t);

                else if (t.T == TokType.Func)
                    ops.Push(t); // functions go on operator stack

                else if (t.T == TokType.Op)
                {
                    while (ops.Count > 0 && ops.Peek().T == TokType.Op &&
                           (Prec(ops.Peek().S) > Prec(t.S) ||
                           (Prec(ops.Peek().S) == Prec(t.S) && !RightAssoc(t.S))))
                        output.Add(ops.Pop());
                    ops.Push(t);
                }
                else if (t.T == TokType.LPar) ops.Push(t);
                else if (t.T == TokType.RPar)
                {
                    while (ops.Count > 0 && ops.Peek().T != TokType.LPar) output.Add(ops.Pop());
                    if (ops.Count == 0) throw new ArgumentException("Mismatched parentheses.");
                    ops.Pop(); // pop '('
                    // if there is a function token on top, pop it to output (unary)
                    if (ops.Count > 0 && ops.Peek().T == TokType.Func) output.Add(ops.Pop());
                }
            }
            while (ops.Count > 0)
            {
                if (ops.Peek().T == TokType.LPar) throw new ArgumentException("Mismatched parentheses.");
                output.Add(ops.Pop());
            }
            return output;
        }

        // -------- Evaluate RPN --------
        private static double EvalRpn(List<Tok> rpn, double x)
        {
            var st = new Stack<double>();
            foreach (var t in rpn)
            {
                if (t.T == TokType.Num) st.Push(t.V);
                else if (t.T == TokType.VarX) st.Push(x);
                else if (t.T == TokType.Func)
                {
                    if (st.Count < 1) throw new ArgumentException("Malformed expression (function).");
                    double a = st.Pop();
                    switch (t.S)
                    {
                        case "sin": st.Push(Math.Sin(a)); break;
                        case "cos": st.Push(Math.Cos(a)); break;
                        default: throw new ArgumentException($"Unknown function {t.S}");
                    }
                }
                else // operator
                {
                    if (st.Count < 2) throw new ArgumentException("Malformed expression (operator).");
                    double b = st.Pop(), a = st.Pop();
                    switch (t.S)
                    {
                        case "+": st.Push(a + b); break;
                        case "-": st.Push(a - b); break;
                        case "*": st.Push(a * b); break;
                        case "/": st.Push(a / b); break;
                        case "^": st.Push(Math.Pow(a, b)); break;
                        default: throw new ArgumentException($"Unknown operator {t.S}");
                    }
                }
            }
            if (st.Count != 1) throw new ArgumentException("Malformed expression (stack).");
            return st.Pop();
        }
    }
}
