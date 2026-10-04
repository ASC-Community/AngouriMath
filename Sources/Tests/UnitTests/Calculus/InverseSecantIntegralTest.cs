//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The antiderivatives of the inverse secant and the inverse cosecant, which the table of
    /// standard integrals had for the other four inverse trigonometric functions and not for
    /// these two.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// They carry a <c>signum</c> where the other four do not, and it is not decoration:
    /// <c>d/dx arcsec(u)</c> is <c>1/(|u| sqrt(u^2 - 1))</c>, so the <c>u</c> that integration by
    /// parts multiplies it by leaves <c>sgn(u)/sqrt(u^2 - 1)</c> rather than
    /// <c>1/sqrt(u^2 - 1)</c>. The domain is two intervals, and the sign is what makes the answer
    /// hold on the left one as well as the right.
    /// </para>
    /// <para>
    /// <b>Every case is checked on both intervals</b>, which is the only way that sign shows up:
    /// an answer without it is right for <c>u &gt; 1</c> and wrong for <c>u &lt; -1</c>.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class InverseSecantIntegralTest
    {
        private static void DifferentiatesBack(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 4,
                $"only {compared} of {points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>
        /// Both functions, both intervals — two points where the argument is above one and two
        /// where it is below minus one.
        /// </summary>
        [Theory]
        [InlineData("asec(x)", new[] { 1.4, 2.6, 5.1, -1.9, -3.7 })]
        [InlineData("acsc(x)", new[] { 1.4, 2.6, 5.1, -1.9, -3.7 })]
        public void BothFunctionsOnBothIntervals(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// A linear argument, which divides the whole answer by the rate. The constant term is
        /// there because a rule reading only the coefficient gets <c>2x + 1</c> right by accident
        /// and <c>2x</c> wrong in the same way.
        /// </summary>
        [Theory]
        [InlineData("asec(3*x)", new[] { 0.5, 1.2, 2.4, -0.6, -1.8 })]
        [InlineData("acsc(2*x)", new[] { 0.7, 1.4, 3.1, -0.9, -2.2 })]
        [InlineData("asec(2*x + 1)", new[] { 0.4, 1.2, 2.4, -1.5, -3.0 })]
        [InlineData("acsc(3*x - 2)", new[] { 1.2, 2.4, 4.1, -0.5, -1.7 })]
        public void ALinearArgument(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// The four that were already in the table, which must be untouched — the two new rows
        /// sit directly beside them.
        /// </summary>
        [Theory]
        [InlineData("arcsin(x)", new[] { 0.3, 0.5, -0.4, -0.8 })]
        [InlineData("arccos(x)", new[] { 0.3, 0.5, -0.4, -0.8 })]
        [InlineData("arctan(x)", new[] { 0.3, 1.5, -0.4, -2.8 })]
        [InlineData("arccotan(x)", new[] { 0.3, 1.5, -0.4, -2.8 })]
        [InlineData("arcsin(2*x)", new[] { 0.2, 0.4, -0.3, -0.45 })]
        public void TheFourThatWereAlreadyThere(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// Charlwood's <c>x^3 arcsec(x)/sqrt(x^4 - 1)</c>, by parts: the remainder is
        /// <c>sqrt(x^4 - 1)/(2 x^2 sqrt(1 - 1/x^2))</c>, with the derivative of the arcsecant
        /// as it is written, which is <c>sqrt(x^2 + 1)/(2|x|)</c> once the root of the quotient
        /// is split, the two roots combined as the quotient they are, and the even power of
        /// <c>x</c> taken out of the root -- three rewritings, each exact, and none of which
        /// read it before. Both intervals of the arcsecant.
        /// </summary>
        [Theory]
        [InlineData("x^3*asec(x)/sqrt(x^4 - 1)", new[] { 1.4, 2.6, 5.1, -1.9, -3.7 })]
        public void ByPartsAgainstARootOfAQuartic(string integrand, double[] points) => DifferentiatesBack(integrand, points);

        /// <summary>
        /// The arcsecant beside a root of <c>x^2 - 1</c>, under <c>x = sec(u)</c>: the root is
        /// <c>tan(u) sgn(x)</c> on the arcsecant's range, and taken as <c>tan(u)</c> the answers
        /// were right for x &gt; 1 and their derivatives the integrands' negatives for every
        /// x &lt; -1, where the integrands are as real. Timofeev's and Charlwood's. With a linear
        /// argument the sign is the argument's: at <c>x = -1.5</c> the sign of <c>x + 3</c> is
        /// not the sign of <c>x</c>.
        /// </summary>
        [Theory]
        [InlineData("x*asec(x)/sqrt(-1 + x^2)", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x)*sqrt(-1 + x^2)/x^4", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x)/(-1 + x^2)^(5/2)", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("x^3*asec(x)/(-1 + x^2)^(5/2)", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x)/(x^2*sqrt(-1 + x^2))", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("(-1 + x^2)^(3/2)*asec(x)^2/x^5", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x)^3*sqrt(-1 + x^2)/x^4", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x)^2/(x*sqrt(x^2 - 1))", new[] { 1.3, 2.6, -1.3, -1.9, -2.7 })]
        [InlineData("asec(x + 3)^2/((x + 3)*sqrt((x + 3)^2 - 1))", new[] { 0.5, -1.5, -0.5, -4.5, -6.0 })]
        public void BesideARootOnBothIntervals(string integrand, double[] points) => DifferentiatesBack(integrand, points);

        /// <summary>
        /// A non-linear argument is not this row's: <c>arcsec(x^2)</c> would want the chain rule
        /// undone first, and the table reads a linear argument only.
        /// </summary>
        [Theory]
        [InlineData("asec(x^2)")]
        [InlineData("acsc(1/x)")]
        public void ANonLinearArgumentIsDeclined(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
        }
    }
}
