//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// An equation between two inverse trigonometric functions of polynomials, <c>f(u) = g(v)</c> or
    /// <c>f(u) = -g(v)</c>, solved by a polynomial equation in their squared sines and cosines.
    /// </summary>
    /// <remarks>
    /// <c>sin(f)^2 cos(g)^2 - cos(f)^2 sin(g)^2</c> is <c>sin(f - g) sin(f + g)</c>, so it is zero
    /// wherever <c>f = g</c> or <c>f = -g</c>. For each of the six functions the squared sine and
    /// cosine are rational in the argument, and polynomial once a factor that is never zero where the
    /// function has a value is taken out: <c>arcsin(u)</c> gives <c>u^2</c> and <c>1 - u^2</c>,
    /// <c>arctan(u)</c> gives <c>u^2</c> and <c>1</c> over <c>1 + u^2</c>. So the polynomial
    /// equation has every root of the original. It also has the roots of <c>f = +-g + k pi</c>, so
    /// each candidate is put back into the equation as written, under the principal branches, and
    /// kept only where it holds. <c>arcsin(-2x) = arccos(3x + 2)</c> is <c>13x^2 + 12x + 3 = 0</c>,
    /// both of whose roots hold. Newton's method answered these with the roots it found, which left
    /// out complex ones.
    /// </remarks>
    internal static class InverseTrigonometricPairSolver
    {
        /// <summary>The highest degree the polynomial equation may have.</summary>
        private const int HighestDegree = 8;

        internal static Set? Solve(Entity expr, Variable x)
        {
            if (expr.Vars.Any(variable => variable != x))
                return null;
            var functions = new List<(ERational Multiplier, Entity Function)>();
            Entity constant = Integer.Zero;
            if (!Gather(expr, ERational.One, x, functions, ref constant)
                || functions.Count != 2
                || !functions[0].Multiplier.Abs().Equals(functions[1].Multiplier.Abs())
                || constant.Evaled is not Integer { IsZero: true })
                return null;

            var degree = 0;
            var parts = new List<(Entity Sine, Entity Cosine)>(2);
            foreach (var (_, function) in functions)
            {
                var argument = ((IUnaryNode)function).NodeChild;
                if (!TreeAnalyzer.TryGetPolynomial(argument, x, out var monomials)
                    || monomials.Keys.Any(exponent => exponent.Sign < 0)
                    || monomials.Values.Any(coefficient => coefficient.ContainsNode(x)))
                    return null;
                degree += 2 * (monomials.Count == 0 ? 0 : monomials.Keys.Max()!.ToInt32Checked());
                if (degree > HighestDegree)
                    return null;
                var square = MathS.Pow(argument, 2);
                parts.Add(function switch
                {
                    Arcsinf => (square, 1 - square),
                    Arccosf => (1 - square, square),
                    Arctanf => (square, Integer.One),
                    Arccotanf => (Integer.One, square),
                    Arcsecantf => (square - 1, Integer.One),
                    _ => (Integer.One, square - 1),
                });
            }
            var product = parts[0].Sine * parts[1].Cosine - parts[0].Cosine * parts[1].Sine;
            return PolynomialCandidates.RootsAmong(expr, x, product, degree);
        }

        /// <summary>
        /// Reads <paramref name="term"/>, times <paramref name="sign"/>, as rational multiples of
        /// inverse trigonometric functions of <paramref name="x"/> and a constant;
        /// <see langword="false"/> where it is anything else.
        /// </summary>
        private static bool Gather(Entity term, ERational sign, Variable x,
            List<(ERational Multiplier, Entity Function)> functions, ref Entity constant)
        {
            switch (term)
            {
                case var _ when !term.ContainsNode(x):
                    constant += Rational.Create(sign) * term;
                    return true;
                case Sumf(var augend, var addend):
                    return Gather(augend, sign, x, functions, ref constant)
                        && Gather(addend, sign, x, functions, ref constant);
                case Minusf(var minuend, var subtrahend):
                    return Gather(minuend, sign, x, functions, ref constant)
                        && Gather(subtrahend, sign.Negate(), x, functions, ref constant);
                case Mulf(Rational multiplier, var rest):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, functions, ref constant);
                case Mulf(var rest, Rational multiplier):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, functions, ref constant);
                case Arcsinf or Arccosf or Arctanf or Arccotanf or Arcsecantf or Arccosecantf when !sign.IsZero:
                    functions.Add((sign, term));
                    return true;
                default:
                    return false;
            }
        }
    }
}
