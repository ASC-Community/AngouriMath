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
    /// A rational function with rational coefficients whose denominator the partial-fraction
    /// splits could not take apart, integrated by the Hermite reduction and the
    /// Rothstein–Trager resultant, with the logarithmic part in real terms after Rioboo.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bronstein's <c>(6 - 3x^2 + x^4)/(4 + 5x^2 - 5x^4 + x^6)</c> had no antiderivative: the
    /// denominator is irreducible over the rationals, and its real quadratic factors carry
    /// the roots of a cubic. The residues are what decide the field the answer needs, and
    /// they are <c>±i/2</c> and <c>±i</c>: the answer is three arctangents of polynomials.
    /// </para>
    /// <para>
    /// The arctangents are Rioboo's, of polynomials rather than of quotients, so the
    /// antiderivative is continuous and the fundamental theorem holds across the whole line.
    /// That is asserted on Bronstein's integrand, whose integral over <c>[-2, 2]</c> is
    /// <c>5 pi/2</c>.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class RothsteinTragerTest
    {
        private static Entity DifferentiatesBack(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

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
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 5,
                $"only {compared} of {points.Length} points were comparable for {integrand}");
            return integral;
        }

        private static readonly double[] TheLine = { -2.3, -1.1, -0.4, 0.3, 0.9, 1.7, 2.6 };

        /// <summary>
        /// Bronstein's sextic and Welz's quartic, both irreducible over the rationals; a
        /// product of an irreducible quadratic and an irreducible quartic, where the
        /// residues of the two parts are rational and in <c>Q(sqrt(-3))</c>; and one with a
        /// repeated factor, for the Hermite reduction in front.
        /// </summary>
        [Theory]
        [InlineData("(6 - 3*x^2 + x^4)/(4 + 5*x^2 - 5*x^4 + x^6)")]
        [InlineData("(-84 - 576*x - 400*x^2 + 2560*x^3)/(9 + 24*x - 12*x^2 + 80*x^3 + 320*x^4)")]
        [InlineData("(x^2 + 3)/((x^2 + x + 1)*(x^4 + x^2 + 3))")]
        [InlineData("(x^5 + x^3 + 1)/((x^2 + 2)^3*(x^4 + 1))")]
        [InlineData("(x^3 + 1)/((x^2 + 1)^2*(x^2 - x + 3))")]
        public void TheResiduesDecideTheField(string integrand) => DifferentiatesBack(integrand, TheLine);

        /// <summary>
        /// Jeffrey's rational functions of the sine and cosine, whose half-angle images have
        /// denominators irreducible over the rationals; each reached this through the
        /// half-angle substitution.
        /// </summary>
        [Theory]
        [InlineData("(1 + cos(x) + 2*sin(x))/(3 + cos(x)^2 + 2*sin(x) - 2*cos(x)*sin(x))")]
        [InlineData("(-1 + 4*cos(x) + 5*cos(x)^2)/(-1 - 4*cos(x) - 3*cos(x)^2 + 4*cos(x)^3)")]
        [InlineData("(-5 + 2*cos(x) + 7*cos(x)^2)/(-1 + 2*cos(x) - 9*cos(x)^2 + 4*cos(x)^3)")]
        public void ThroughTheHalfAngle(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.3, 0.7, 1.1, 1.6, 2.0, 2.6 });

        /// <summary>
        /// The antiderivative is continuous, being arctangents of polynomials and not of
        /// quotients, so the fundamental theorem holds across the line: Bronstein's integrand
        /// over <c>[-2, 2]</c> is <c>5 pi/2</c>.
        /// </summary>
        [Fact]
        public void TheArctangentsAreContinuous()
        {
            var integral = "(6 - 3*x^2 + x^4)/(4 + 5*x^2 - 5*x^4 + x^6)".ToEntity().Integrate("x").Substitute("C", 0);
            var over = (double)(integral.Substitute("x", 2).EvalNumerical() - integral.Substitute("x", -2).EvalNumerical()).RealPart;
            Assert.True(Math.Abs(over - 5 * Math.PI / 2) < 1e-9, $"F(2) - F(-2) is {over}, where the integral is 5 pi/2");
        }

        /// <summary>
        /// A residue in a field of degree three or more puts its logarithms in a sum over the poles
        /// it belongs to, <c>sum(A(r)/D'(r) ln(x - r), r in { r : E(r) = 0 })</c>, where these
        /// were declined. <c>x^5 + x + 1</c> is <c>(x^2 + x + 1)(x^3 - x^2 + 1)</c>, and the
        /// quadratic factor's residues go into the sum beside the cubic's.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a>
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + x + 1)")]
        [InlineData("x/(x^3 - x + 1)")]
        [InlineData("(x^2 + 3)/(x^5 + x + 1)")]
        [InlineData("1/(x^5 - x + 1)")]
        [InlineData("x^2/(x^4 + x + 1)")]
        [InlineData("x^2/(x^4 + x^3 + 1)")]
        public void AResidueOfDegreeThreeIsASumOverRoots(string integrand)
            => Assert.Contains(DifferentiatesBack(integrand, TheLine).Nodes, node => node is Entity.SumOverSetf);
    }
}
