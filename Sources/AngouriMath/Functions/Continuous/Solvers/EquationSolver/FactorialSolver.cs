//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath.Extensions;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// <c>c (x + k)! + P(x) = 0</c>, with <c>c</c> a rational that is not zero, <c>k</c> a whole
    /// number and <c>P</c> a polynomial over <c>Q</c>: its whole roots, exactly, and the rest of
    /// its roots left as the equation. <c>gamma(x + k)</c> is <c>(x + k - 1)!</c>, so this is the
    /// gamma function's equation as well. https://github.com/asc-community/AngouriMath/issues/2538
    /// </summary>
    /// <remarks>
    /// <para>
    /// Where <c>P</c> is not 0 there are infinitely many roots, so no finite set is the answer.
    /// The factorial has a pole at each negative whole number and tends to the same infinity on
    /// both sides of the interval between two of them, while the least of its size there tends
    /// to 0; on every other such interval far enough out, its sign is the sign of
    /// <c>-P / c</c> there, so the equation changes sign twice in it. <c>x + x! = x^3</c> has
    /// two real roots in each of (-4, -3), (-6, -5), (-8, -7) and so on, and Newton's method,
    /// started inside [-10, 10]^2, answered 13 decimals without -3.0200.
    /// </para>
    /// <para>
    /// The whole roots are found by trying each whole number from the first at which the
    /// factorial is defined, exactly, and the search ends with a proof that no larger one is a
    /// root. For <c>n &gt;= 1</c>, <c>|P(n)| &lt;= S n^d</c>, where <c>d</c> is the degree and
    /// <c>S</c> the sum of the sizes of the coefficients. For <c>n &gt;= d</c>,
    /// <c>((n + 1) / n)^d &lt;= (1 + 1/d)^d &lt; 3</c>, so once <c>(n + k)! &gt;= 2</c> as well,
    /// a step multiplies the factorial by <c>n + k + 1 &gt;= 3</c> and the bound by less. The
    /// first <c>n</c> past both at which <c>|c| (n + k)! &gt; S n^d</c> is therefore the last
    /// one there is any need to try.
    /// </para>
    /// </remarks>
    internal static class FactorialSolver
    {
        /// <summary>
        /// How many whole numbers are tried before this declines. The search ends within about
        /// <c>d + 20</c> of them for a polynomial whose coefficients have fewer than a few
        /// hundred digits, so this stops only an equation it was not written for.
        /// </summary>
        private const int MostWholeNumbersTried = 1000;

        internal static Set? Solve(Entity expr, Variable x)
        {
            Factorialf? factorial = null;
            var multiplier = ERational.One;
            var rest = new List<Entity>();
            foreach (var term in Sumf.LinearChildren(expr))
            {
                if (!term.Nodes.Any(node => node is Factorialf && node.ContainsNode(x)))
                {
                    rest.Add(term);
                    continue;
                }
                if (factorial is not null)
                    return null;
                foreach (var factor in Mulf.LinearChildren(term))
                    if (factor is Factorialf found && factorial is null)
                        factorial = found;
                    else if (!factor.ContainsNode(x) && factor.InnerSimplified is Rational number)
                        multiplier = multiplier.Multiply(number.ERational);
                    else
                        return null;
            }
            if (factorial is null || multiplier.IsZero
                || !TreeAnalyzer.TryGetPolyLinear(factorial.Argument, x, out var slope, out var offset)
                || slope.InnerSimplified is not Integer { EInteger: var one } || !one.Equals(EInteger.One)
                || offset.InnerSimplified is not Integer { EInteger: var shiftBy }
                || !shiftBy.CanFitInInt32() || shiftBy.Abs().CompareTo(EInteger.FromInt32(MostWholeNumbersTried)) > 0)
                return null;
            var shift = shiftBy.ToInt32Checked();

            // P's coefficients, lowest power first.
            var coefficients = new List<ERational>();
            if (rest.Count > 0)
            {
                if (!TreeAnalyzer.TryGetPolynomial(rest.Aggregate((sum, term) => sum + term), x, out var monomials))
                    return null;
                foreach (var monomial in monomials)
                {
                    var power = monomial.Key;
                    if (power.Sign < 0 || !power.CanFitInInt32() || monomial.Value.InnerSimplified is not Rational value)
                        return null;
                    var at = power.ToInt32Checked();
                    if (at >= MostWholeNumbersTried)
                        return null;
                    while (coefficients.Count <= at)
                        coefficients.Add(ERational.Zero);
                    coefficients[at] = coefficients[at].Add(value.ERational);
                }
            }
            while (coefficients.Count > 0 && coefficients[coefficients.Count - 1].IsZero)
                coefficients.RemoveAt(coefficients.Count - 1);

            // The factorial is 0 nowhere, the gamma function having no zeros.
            if (coefficients.Count == 0)
                return Empty;

            var degree = coefficients.Count - 1;
            var size = coefficients.Aggregate(ERational.Zero, (sum, coefficient) => sum.Add(coefficient.Abs()));
            var scale = multiplier.Abs();

            var whole = new List<Entity>();
            var n = -shift;
            var factorialOfM = EInteger.One;   // (n + shift)!, starting at 0!
            var settled = false;
            for (var tried = 0; tried < MostWholeNumbersTried; tried++, n++)
            {
                var m = n + shift;
                var atN = ERational.Zero;
                var power = ERational.FromInt32(n);
                for (var i = degree; i >= 0; i--)
                    atN = atN.Multiply(power).Add(coefficients[i]);
                if (multiplier.Multiply(ERational.FromEInteger(factorialOfM)).Add(atN).IsZero)
                    whole.Add(Integer.Create(n));
                if (n >= 1 && n >= degree && m >= 2
                    && scale.Multiply(ERational.FromEInteger(factorialOfM))
                        .CompareTo(size.Multiply(ERational.FromEInteger(EInteger.FromInt32(n).Pow(degree)))) > 0)
                {
                    settled = true;
                    break;
                }
                factorialOfM = factorialOfM.Multiply(EInteger.FromInt32(m + 1));
            }
            if (!settled)
                return null;

            Entity condition = expr is Minusf(var lhs, var rhs) ? lhs.Equalizes(rhs) : expr.Equalizes(0);
            foreach (var root in whole)
                condition &= !x.Equalizes(root);
            var unsolved = new ConditionalSet(x, condition);
            return whole.Count == 0 ? unsolved : new Set[] { new FiniteSet(whole), unsolved }.Unite();
        }
    }
}
