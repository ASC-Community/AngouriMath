//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <see cref="Entity.Integrate(Entity.Variable)"/> returned piecewise answers whose
    /// branches were left unreduced, guarded by comparisons of two constants nobody had
    /// evaluated:
    /// <code>
    ///     1/sqrt(x^2 - 1)  ->  piecewise((1 * x / sqrt(-1))          provided (1 * 1 ^ 2 = 0),
    ///                                    (... arcsin ...)            provided (1 * 1 ^ 2 &lt; 0),
    ///                                    (... ln ...)                provided (1 * 1 ^ 2 &gt; 0))
    /// </code>
    /// The value was right -- the third branch is the live one -- but two dead branches and
    /// every literal coefficient survived into the answer, so anything that inspects rather
    /// than evaluates the result saw them.
    /// https://github.com/asc-community/AngouriMath/issues/772
    /// </summary>
    /// <remarks>
    /// <see cref="Entity.Piecewise"/> already drops a branch whose guard evaluates to false
    /// and stops at one that evaluates to true. The indefinite integral simply never asked
    /// it to: it inner-simplifies its *input* and returns its output untouched, while the
    /// definite overload has always inner-simplified the antiderivative.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class IntegralPiecewiseReducedTest
    {
        /// <summary>
        /// Every guard here is a comparison of literals, so no branch is in doubt and no
        /// piecewise should reach the caller at all.
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(x^2 - 1)")]
        [InlineData("1/sqrt(x^2 - 4)")]
        [InlineData("10/sqrt(x^2 - 4) + 1/sqrt(x^2 - 1)")]
        [InlineData("1/sqrt(x^2 - a^2)")]
        [InlineData("1/(x*sqrt(ln(x)^2 - a^2))")]
        public void ADecidablePiecewiseDoesNotReachTheCaller(string integrand)
        {
            var antiderivative = integrand.ToEntity().Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            Assert.DoesNotContain(antiderivative.Nodes, node => node is Entity.Piecewise);
            Assert.DoesNotContain(antiderivative.Nodes, node => node == MathS.NaN);
        }

        /// <summary>
        /// The guard must be *decided*, not merely dropped. A symbolic coefficient leaves
        /// the sign genuinely open, and those answers have to keep their piecewise -- a
        /// reduction that threw the branches away here would be answering a question it
        /// cannot decide. https://github.com/asc-community/AngouriMath/issues/771
        /// </summary>
        [Theory]
        [InlineData("1/(a + b*x^2)")]
        [InlineData("1/sqrt(b*x^2 - a^2)")]
        public void AnUndecidablePiecewiseIsKept(string integrand)
        {
            var antiderivative = integrand.ToEntity().Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            Assert.Contains(antiderivative.Nodes, node => node is Entity.Piecewise);
        }

        /// <summary>
        /// The reduced answer must still be the same antiderivative. Checked by
        /// differentiating it back rather than against a printed form, since the printed
        /// form is exactly what this changes -- and sampled rather than simplified to a
        /// symbolic zero: differentiating a logarithm of an absolute value gives
        /// <c>sgn(u) / abs(u)</c>, which <see cref="Entity.Simplify"/> does not reduce to
        /// <c>1/u</c>, so a symbolic assertion here would measure the simplifier.
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(x^2 - 1)", new[] { -4.1, -2.5, 1.4, 2.3, 3.9 })]
        [InlineData("1/sqrt(x^2 - 4)", new[] { -5.2, -3.1, 2.6, 3.4, 4.8 })]
        [InlineData("1/(1 + x^2)", new[] { -2.2, -0.7, 0.5, 1.3, 3.6 })]
        public void ReducingDoesNotChangeTheAntiderivative(string integrand, double[] points)
        {
            var derivative = integrand.ToEntity().Integrate("x").Substitute("C", 0).Differentiate("x");
            var compared = 0;
            foreach (var point in points)
            {
                if (!TryEval(derivative, point, out var actual)) continue;
                if (!TryEval(integrand.ToEntity(), point, out var expected)) continue;
                compared++;
                var scale = System.Math.Max(System.Math.Max(
                    System.Math.Abs(expected), System.Math.Abs(actual)), 1e-12);
                Assert.True(System.Math.Abs(expected - actual) <= 1e-8 * scale,
                    $"{integrand} at x = {point}: differentiated back to {actual}, integrand is {expected}");
            }
            Assert.True(compared > 0, $"{integrand}: no point was comparable, so nothing was checked");
        }

        /// <summary>
        /// A point counts only where both sides are real and finite -- these integrands
        /// leave the reals over part of the line, and a skipped point is not a failure.
        /// </summary>
        private static bool TryEval(Entity expr, double point, out double value)
        {
            value = 0;
            try
            {
                var evaluated = expr.Substitute("x", point).EvalNumerical();
                var real = evaluated.RealPart.EDecimal.ToDouble();
                if (System.Math.Abs(evaluated.ImaginaryPart.EDecimal.ToDouble())
                    > 1e-9 * System.Math.Max(1, System.Math.Abs(real))) return false;
                if (double.IsNaN(real) || double.IsInfinity(real)) return false;
                value = real;
                return true;
            }
            catch { return false; }
        }
    }
}
