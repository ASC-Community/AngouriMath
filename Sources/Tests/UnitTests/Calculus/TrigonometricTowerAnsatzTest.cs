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
    /// A rational function of <c>x</c> and of <c>sin(x)</c> and <c>cos(x)</c> together, with an
    /// exponential <c>e^(a x)</c> in front or not, integrated by the ansatz <c>F = e^(a x) P/Q</c>
    /// with <c>P</c> and <c>Q</c> polynomials in the three.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Timofeev's <c>x^2/(x cos(x) - sin(x))^2</c> is <c>((x sin(x) + cos(x))/(x cos(x) - sin(x)))'</c>
    /// and his <c>(2x + sin(2x))/(cos(x) + x sin(x))^2</c> is <c>(2x sin(x)/(x sin(x) + cos(x)))'</c>;
    /// neither had an antiderivative, since no substitution reads them and by parts goes
    /// round in a circle. The ring of polynomials in the three is
    /// <c>Q[x, s, c]/(s^2 + c^2 - 1)</c>, closed under the derivative, and <c>F' = N/D</c> is
    /// one identity in it, linear in the coefficients of <c>P</c>.
    /// </para>
    /// <para>
    /// Every answer is differentiated back and compared to the integrand numerically; the
    /// non-elementary neighbours -- <c>sin(x)/x</c>, <c>x/sin(x)</c>, <c>x tan(x)</c> -- are
    /// pinned as declined, since answering them would be a wrong answer and not a missing one.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class TrigonometricTowerAnsatzTest
    {
        private static readonly double[] Points = { 0.3, 0.9, 1.7, 2.6 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var compared = 0;
            foreach (var at in Points)
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
            Assert.True(compared >= 3,
                $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// Timofeev's two, and the derivatives of <c>sin(x)/x</c>, of <c>tan(x)/x</c> and of
        /// <c>e^x sin(x)/x</c>; the double angle is read as the ring's element <c>2 s c</c>.
        /// </summary>
        [Theory]
        [InlineData("x^2/(x*cos(x) - sin(x))^2")]
        [InlineData("(2*x + sin(2*x))/(cos(x) + x*sin(x))^2")]
        [InlineData("(x*cos(x) - sin(x))/x^2")]
        [InlineData("(x - sin(x)*cos(x))/(x^2*cos(x)^2)")]
        [InlineData("e^x*(x*sin(x) + x*cos(x) - sin(x))/x^2")]
        public void ARationalFunctionOfTheThree(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The answer is the ring's, not the half-angle tangent's: Timofeev's comes back as
        /// he gives it, up to the sign of the quotient.
        /// </summary>
        [Fact]
        public void TheAnswerIsInTheSineAndCosine()
        {
            var integral = "x^2/(x*cos(x) - sin(x))^2".ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("tan(", integral);
            Assert.Contains("x * sin(x)", integral);
        }

        /// <summary>
        /// What has no elementary antiderivative is declined, not answered: the ansatz finds
        /// no <c>P</c> for any <c>Q</c>, and nothing else reads the shape.
        /// </summary>
        [Theory]
        [InlineData("tan(x)/x")]
        [InlineData("x/sin(x)")]
        [InlineData("x*tan(x)")]
        [InlineData("x*sin(x)/(1 + x + x^3)")]
        public void ANonElementaryOneIsDeclined(string integrand)
            => Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());
    }
}
