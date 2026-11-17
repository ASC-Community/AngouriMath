//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// The principal <c>arcosh(C)</c> is <c>ln(C + sqrt(C + 1) sqrt(C - 1))</c>, and <c>arsech</c> the
    /// same of <c>1/C</c> (DLMF 4.37.19). The integrator's routes read the inverse hyperbolic cosine
    /// as <c>ln(C + sqrt(C^2 - 1))</c>, which is the same function wherever <c>Re C &gt;= 0</c> and its
    /// negation elsewhere -- on the real line, only where <c>C &lt; -1</c>, where neither is real. So
    /// an integrand is asked in the single-root form, and the answer written back in the principal
    /// one for the arguments that were rewritten, which keeps every antiderivative on the real
    /// domain of its integrand. A single root the integrator writes for its own reasons is left as
    /// it is: <c>ln(x + sqrt(x^2 - 1))</c> for <c>1/sqrt(x^2 - 1)</c> is right at <c>x &lt; -1</c>,
    /// where the principal form would change its sign.
    /// </summary>
    internal static class PrincipalInverseHyperbolicCosine
    {
        /// <summary>
        /// The integrand with each principal inverse hyperbolic cosine written with one root, and
        /// the arguments rewritten; the same entity and no arguments where there was none.
        /// </summary>
        internal static (Entity Expr, IReadOnlyList<Entity> Centers) ToTheSingleRoot(Entity expr)
        {
            if (!expr.Nodes.Any(node => node is Logf(var @base, Sumf) && @base == MathS.e))
                return (expr, System.Array.Empty<Entity>());
            var centers = new List<Entity>();
            var rewritten = expr.Replace(node =>
            {
                if (node is Logf(var @base, Sumf(var left, var right)) && @base == MathS.e)
                    foreach (var (center, radical) in new[] { (left, right), (right, left) })
                        if (IsTheRootPairAbout(radical, center))
                        {
                            if (!centers.Contains(center))
                                centers.Add(center);
                            return MathS.Ln(center + MathS.Sqrt(MathS.Sqr(center) - 1));
                        }
                return node;
            });
            return centers.Count == 0 ? (expr, centers) : (rewritten, centers);
        }

        /// <summary>
        /// The answer with each <c>ln(C + sqrt(C^2 - 1))</c> of a rewritten argument <c>C</c>
        /// written back as the principal <c>ln(C + sqrt(C + 1) sqrt(C - 1))</c>.
        /// </summary>
        internal static Entity ToThePrincipal(Entity answer, IReadOnlyList<Entity> centers)
            => centers.Count == 0 ? answer : answer.Replace(node =>
            {
                if (node is Logf(var @base, Sumf(var left, var right)) && @base == MathS.e)
                    foreach (var (center, radical) in new[] { (left, right), (right, left) })
                        if (centers.FirstOrDefault(known => SameValue(known, center)) is { } known
                            && radical is Powf(var radicand, Number.Rational half) && half == Number.Rational.Create(1, 2)
                            && SameValue(radicand, MathS.Sqr(known) - 1))
                            return MathS.Ln(known + ThePair(known));
                // The routes write sinh(u) for the argument's root, `sqrt(C^2 - 1)`, and for the
                // principal arcosh sinh(arcosh(C)) is `sqrt(C + 1) sqrt(C - 1)` at every C. The two
                // agree on the real line but where C < -1, so this costs no real domain anything.
                if (node is Powf(var power, Number.Rational exponent) && exponent is not Number.Integer
                    && exponent.ERational.Denominator.Equals(PeterO.Numbers.EInteger.FromInt32(2))
                    && power is Sumf or Minusf
                    && centers.FirstOrDefault(known => SameValue(power, MathS.Sqr(known) - 1)) is { } squared)
                    return MathS.Pow(ThePair(squared), Number.Integer.Create(exponent.ERational.Numerator));
                return node;
            });

        private static Entity ThePair(Entity center) => MathS.Sqrt(center + 1) * MathS.Sqrt(center - 1);

        // `sqrt(center + 1) sqrt(center - 1)`, in either order.
        private static bool IsTheRootPairAbout(Entity radical, Entity center)
        {
            if (radical is not Mulf(Powf(var first, Number.Rational firstHalf), Powf(var second, Number.Rational secondHalf))
                || firstHalf != Number.Rational.Create(1, 2) || secondHalf != Number.Rational.Create(1, 2))
                return false;
            return SameValue(first, center + 1) && SameValue(second, center - 1)
                || SameValue(second, center + 1) && SameValue(first, center - 1);
        }

        // As trees, or by value at two assignments of every symbol: `a^2 x^2 - 1` against
        // `(a x)^2 - 1` is not settled by the inner simplification.
        private static bool SameValue(Entity a, Entity b)
        {
            if (a == b || (a - b).InnerSimplified is Number.Complex { IsZero: true })
                return true;
            var symbols = a.Vars.Concat(b.Vars).Distinct().ToList();
            if (symbols.Count == 0)
                return false;
            foreach (var pins in new[] { new[] { "0.61", "1.37", "2.23", "0.83", "1.91" }, new[] { "1.13", "0.47", "1.79", "2.71", "0.29" } })
            {
                var difference = a - b;
                for (var index = 0; index < symbols.Count; index++)
                    difference = difference.Substitute(symbols[index], Number.Real.Create(PeterO.Numbers.EDecimal.FromString(pins[index % pins.Length])));
                if (difference.Evaled is not Number.Complex value || value.IsNaN
                    || value.RealPart.EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1e-20")) > 0
                    || value.ImaginaryPart.EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1e-20")) > 0)
                    return false;
            }
            return true;
        }
    }
}
