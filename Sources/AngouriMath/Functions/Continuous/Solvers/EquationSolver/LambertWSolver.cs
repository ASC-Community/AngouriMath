//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// <c>K (alpha x + beta) b^(gamma x + delta) = c</c>, a linear function times an exponential of
    /// another, solved by the Lambert W function. https://github.com/asc-community/AngouriMath/issues/2182
    /// </summary>
    /// <remarks>
    /// Written <c>e^((gamma x + delta) ln b)</c>, the exponent is <c>u = g x + d</c>, and with
    /// <c>s = beta g / alpha - d</c> the equation is <c>(u + s) e^(u + s) = A</c>,
    /// <c>A = c g e^s / (K alpha)</c>, so <c>u + s = W(A)</c> and <c>x = W(A) / g - beta / alpha</c>.
    /// Over the complex numbers every branch is a root, <c>W_n</c> for a whole <c>n</c>, as a periodic
    /// equation's roots are written. Over the reals two branches can be: <c>W_0</c> where
    /// <c>A &gt;= -1/e</c> and <c>W_(-1)</c> where <c>-1/e &lt;= A &lt; 0</c>, and none where
    /// <c>A &lt; -1/e</c>, since <c>w e^w &gt;= -1/e</c> for every real <c>w</c>.
    /// </remarks>
    internal static class LambertWSolver
    {
        internal static Set? Solve(Entity expr, Variable x)
        {
            var terms = Sumf.LinearChildren(expr);
            var withX = terms.Where(term => term.ContainsNode(x)).ToList();
            if (withX.Count != 1)
                return null;
            var c = (-terms.Where(term => !term.ContainsNode(x)).Aggregate((Entity)Integer.Zero, (sum, term) => sum + term)).InnerSimplified;

            Entity k = Integer.One;
            Entity? linear = null, exponent = null, logBase = null;
            foreach (var factor in Mulf.LinearChildren(withX[0]))
            {
                if (!factor.ContainsNode(x))
                    k *= factor;
                else if (factor is Powf(var @base, var power) && !@base.ContainsNode(x))
                {
                    if (exponent is not null)
                        return null;
                    exponent = power;
                    logBase = @base == MathS.e ? Integer.One : MathS.Ln(@base);
                }
                else if (linear is null)
                    linear = factor;
                else
                    return null;
            }
            if (linear is null || exponent is null || logBase is null
                || !TreeAnalyzer.TryGetPolyLinear(linear, x, out var alpha, out var beta)
                || !TreeAnalyzer.TryGetPolyLinear(exponent, x, out var gammaRaw, out var deltaRaw))
                return null;
            // The multiplier, the slope and the rate are divided by, so each is to be a number that is
            // not zero; what they are otherwise is a case split this does not make.
            k = k.InnerSimplified;
            var gamma = (gammaRaw * logBase).InnerSimplified;
            var delta = (deltaRaw * logBase).InnerSimplified;
            if (!IsANonZeroNumber(k) || !IsANonZeroNumber(alpha) || !IsANonZeroNumber(gamma))
                return null;

            // Simplified, both small, so that the root reads W(6 e^(-5/2)) / 3 + 1/2.
            var s = (beta * gamma / alpha - delta).InnerSimplified;
            var a = (c * gamma * MathS.Pow(MathS.e, s) / (k * alpha)).Simplify();
            var shift = (-beta / alpha).Simplify();
            Entity Root(Entity branch) => MathS.LambertW(branch, a) / gamma + shift;

            if (MathS.Settings.Codomain.Value != Core.Domain.Real)
                return new FiniteSet(Root(Variable.CreateUnique(expr + a, "n")));

            var aboveTheBranchPoint = (a + 1 / MathS.e).InnerSimplified;
            if (aboveTheBranchPoint.Evaled is Real { EDecimal: var distance } && a.Evaled is Real { EDecimal: var at })
            {
                if (distance.IsNegative)
                    return Empty;
                if (distance.IsZero || !at.IsNegative)
                    return new FiniteSet(Root(Integer.Zero));
                return new FiniteSet(Root(Integer.Zero), Root(Integer.MinusOne));
            }
            return new FiniteSet(
                Root(Integer.Zero).Provided(a >= -1 / MathS.e),
                Root(Integer.MinusOne).Provided((a >= -1 / MathS.e) & (a < 0)));
        }

        private static bool IsANonZeroNumber(Entity e) => e.Evaled is Complex number && !number.IsZero;
    }
}
