//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A rational function of <c>x</c> and one square root of a cubic or a quartic,
    /// integrated by the ansatz over logarithms of <c>A - B y</c> and arctangents of
    /// <c>A/(B y)</c> whose <c>A/B</c> is a Padé approximant of a branch of <c>y</c> at a pole.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The curve is elliptic, so no substitution rationalises it, and the ones with an
    /// elementary antiderivative are the pseudo-elliptic integrals: Welz's
    /// <c>(1 + x)/((x - 2) sqrt(1 + x^3))</c> is a logarithm of
    /// <c>(1 + x)^2/3 - sqrt(1 + x^3)</c>, the Taylor polynomial of the root at the pole,
    /// and Bronstein's <c>x/sqrt(x^4 + 10x^2 - 96x - 71)</c> a logarithm of
    /// <c>A - B y</c> with <c>A</c> of degree eight and <c>B</c> of degree six, matched at
    /// infinity, whose norm <c>A^2 - B^2 P</c> is a constant.
    /// </para>
    /// <para>
    /// Every answer is differentiated back at points where the radicand is positive.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SquareRootLogarithmAnsatzTest
    {
        private static void DifferentiatesBack(string integrand, double[] points)
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
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 5,
                $"only {compared} of {points.Length} points were comparable for {integrand}");
        }

        private static readonly double[] AboveMinusOne = { -0.7, -0.3, 0.2, 0.5, 0.8, 1.6, 2.4, 3.5 };

        /// <summary>
        /// Welz's with a rational part, <c>(2/3) sqrt(1 + x^3)</c>, brought over the denominator:
        /// a logarithm at the pole <c>2</c> where <c>1 + x^3</c> is <c>9</c>,
        /// <c>(2/3) ln((1 + x)^2/3 - sqrt(1 + x^3)) - ln(x - 2) - ln(1 + x)/3</c>.
        /// </summary>
        [Theory]
        [InlineData("(x^3-2*x^2+x+1)/((x-2)*sqrt(1+x^3))")]
        public void ALogarithmAtAPole(string integrand)
        {
            DifferentiatesBack(integrand, AboveMinusOne);
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            Assert.Contains("ln(", integral);
            Assert.Contains("ln(x - 2)", integral);
        }

        /// <summary>
        /// Welz's own, a linear over a linear, and as a sum: one of the pseudo-elliptic quotients
        /// a closed rule answers before the ansatz is asked, as
        /// <c>-(2/3) artanh((1 + x)^2/(3 sqrt(1 + x^3)))</c>, one logarithm where the ansatz
        /// writes three.
        /// </summary>
        [Theory]
        [InlineData("(1+x)/((x-2)*sqrt(1+x^3))")]
        [InlineData("(1+x)/((x-2)*sqrt(1+x^3)) + x^2/sqrt(1+x^3)")]
        public void WelzsIsOneLogarithm(string integrand)
        {
            DifferentiatesBack(integrand, AboveMinusOne);
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            Assert.Equal(1, integral.Split("ln(").Length - 1);
        }

        /// <summary>
        /// Bronstein's, matched at infinity: the answer is
        /// <c>-ln(A - B sqrt(P))/8</c> with <c>A = x^8 + 20x^6 - 128x^5 + 54x^4 - 1408x^3 + 3124x^2 + 10001</c>
        /// and <c>B = x^6 + 15x^4 - 80x^3 + 27x^2 - 528x + 781</c>, and nothing else -- the
        /// norm is a constant, so no pole is left for a logarithm of a linear factor.
        /// </summary>
        [Fact]
        public void ALogarithmMatchedAtInfinity()
        {
            DifferentiatesBack("x/sqrt(x^4+10*x^2-96*x-71)", new[] { -4.0, -3.0, -2.0, -1.0, 5.0, 6.0, 7.0 });
            var integral = "x/sqrt(x^4+10*x^2-96*x-71)".ToEntity().Integrate("x").Stringize();
            Assert.Contains("10001", integral);
            Assert.Contains("781", integral);
            Assert.Equal(1, integral.Split("ln(").Length - 1);
        }

        /// <summary>
        /// Where the radicand is the negative of a square at the pole the branch is imaginary
        /// and the pair of logarithms is one arctangent: <c>1/((x + 1) sqrt(x^3 + 3x^2 + 3x))</c>
        /// is <c>-(2/3) arctan(1/sqrt(x^3 + 3x^2 + 3x))</c>.
        /// </summary>
        [Fact]
        public void AnArctangentAtAPole()
        {
            DifferentiatesBack("1/((x+1)*sqrt(x^3+3*x^2+3*x))", new[] { 0.2, 0.5, 0.8, 1.6, 2.4, 3.5 });
            var integral = "1/((x+1)*sqrt(x^3+3*x^2+3*x))".ToEntity().Integrate("x").Stringize();
            Assert.Contains("arctan(", integral);
            Assert.DoesNotContain("ln(", integral);
        }

        /// <summary>
        /// An irreducible quadratic factor of the denominator is a pair of conjugate places,
        /// and where the radicand is a constant modulo it the branch begins with a linear
        /// <c>L</c>, <c>L^2 = m</c> modulo the quadratic: Welz's
        /// <c>x/((8 + x^3) sqrt(x^3 - 1))</c> has <c>sqrt(3)(x - 1)</c> at <c>x^2 - 2x + 4</c>,
        /// where <c>x^3 - 1</c> is <c>-9</c>, beside two arctangents at the pole <c>-2</c> -- one
        /// logarithm, the conjugate difference over <c>sqrt(3)</c>, since the conjugate sum is
        /// the logarithm of the norm and that is the places'; and
        /// <c>(x^2 - 1)/((x^2 + 1) sqrt(x^3 + x^2 + x))</c> has <c>x</c> at <c>x^2 + 1</c>,
        /// <c>2 ln(sqrt(x^3 + x^2 + x) - x) - ln(x) - ln(x^2 + 1)</c>, over the rationals.
        /// </summary>
        [Fact]
        public void ALogarithmAtAQuadraticFactor()
        {
            DifferentiatesBack("x/((8+x^3)*sqrt(-1+x^3))", new[] { 1.3, 1.7, 2.2, 3.0, 4.0, 5.5 });
            var welz = "x/((8+x^3)*sqrt(-1+x^3))".ToEntity().Integrate("x").Stringize();
            Assert.Contains("sqrt(3)", welz);
            Assert.Contains("arctan(", welz);
            Assert.Equal(1, welz.Split("ln(").Length - 1);

            DifferentiatesBack("(x^2-1)/((x^2+1)*sqrt(x^3+x^2+x))", new[] { 0.2, 0.5, 0.8, 1.6, 2.4, 3.5 });
            var rational = "(x^2-1)/((x^2+1)*sqrt(x^3+x^2+x))".ToEntity().Integrate("x").Stringize();
            Assert.Contains("ln(sqrt(x ^ 3 + x ^ 2 + x) - x)", rational);
            Assert.DoesNotContain("sqrt(3)", rational);
        }

        /// <summary>
        /// Where the radicand is not a constant modulo the quadratic the line is still there,
        /// its slope a root of a quadratic, and the branch is lifted to the fifth power of
        /// the factor: Hearn's <c>(2x^6 + 4x^5 + 7x^4 - 3x^3 - x^2 - 8x - 8)/((2x^2 - 1)^2 sqrt(x^4 + 4x^3 + 2x^2 + 1))</c>
        /// has <c>1/2 + 2x</c> at <c>x^2 - 1/2</c>, where the radicand is <c>9/4 + 2x</c>, and
        /// the answer is one logarithm of <c>A - B y</c> with <c>A</c> of degree five and <c>B</c>
        /// of degree three, whose norm is <c>-4(2x^2 - 1)^5</c>, beside <c>ln(2x^2 - 1)</c> and
        /// the rational part <c>(2x + 1) y/(4(x^2 - 1/2))</c>.
        /// </summary>
        [Fact]
        public void ALogarithmLiftedAtAQuadraticFactor()
        {
            var integrand = "(-8-8*x-x^2-3*x^3+7*x^4+4*x^5+2*x^6)/((-1+2*x^2)^2*sqrt(1+2*x^2+4*x^3+x^4))";
            DifferentiatesBack(integrand, new[] { -0.5, 0.2, 0.5, 1.0, 2.0, 3.0 });
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            Assert.Contains("ln(x ^ 2 + -1/2)", integral);
            Assert.Contains("x ^ 5", integral);
            Assert.Equal(2, integral.Split("ln(").Length - 1);
        }

        /// <summary>
        /// <c>ln(A - By)</c> and <c>ln(By - A)</c> have the same derivative, and the one that
        /// is positive where the radicand is, at a sample point, is the one written.
        /// </summary>
        [Fact]
        public void TheArgumentOfTheLogarithmIsWrittenPositive()
        {
            // `x^3/sqrt(x^4 + x^2 + 1)` itself is the substitution's now, `u = x^2`; a radicand
            // with odd powers in it is not, and `(2x + 1)/sqrt((x^2 + x)^2 + 1)` is the ansatz's,
            // matched at infinity with the constant norm `-1`: `ln(sqrt(...) - x^2 - x)`, where
            // `x^2 + x - sqrt(...)` is negative for every real x.
            DifferentiatesBack("(2*x+1)/sqrt(x^4+2*x^3+x^2+1)", new[] { -2.0, -1.0, -0.5, 0.5, 1.0, 2.0 });
            var integral = "(2*x+1)/sqrt(x^4+2*x^3+x^2+1)".ToEntity().Integrate("x");
            Assert.Contains("ln(sqrt(x ^ 4 + 2 * x ^ 3 + x ^ 2 + 1) - ", integral.Stringize());
            var atOne = integral.Substitute("C", 0).Substitute("x", 1).EvalNumerical();
            Assert.True(atOne.ImaginaryPart.EvalNumerical().Abs() < 1e-12, $"not real at 1: {atOne}");
        }

        /// <summary>
        /// A quadratic radicand too, where Euler's substitution leaves a rational function
        /// whose residues lie in a field of degree four: Timofeev's
        /// <c>(3 + x)/((1 + x^2) sqrt(1 + x + x^2))</c> is a logarithm and an arctangent of
        /// <c>(1 +- x)/(sqrt(2) sqrt(1 + x + x^2))</c>, the line <c>(1 + x)/sqrt(2)</c> at
        /// <c>x^2 + 1</c>; his <c>(1 + 2x)/((4 + 4x + 3x^2) sqrt(x^2 + 6x - 1))</c> has its two
        /// lines over two different fields, <c>sqrt(7)(1 + x)</c> and <c>sqrt(7/2)(2 - x)</c>,
        /// each contributing the conjugate difference of its logarithms, which is rational.
        /// </summary>
        [Theory]
        [InlineData("(3+x)/((1+x^2)*sqrt(1+x+x^2))", new[] { -2.0, -0.5, 0.3, 1.0, 2.0, 4.0 }, "sqrt(2)")]
        [InlineData("x/((4+x+x^2)*sqrt(5+4*x+4*x^2))", new[] { -2.0, -0.5, 0.3, 1.0, 2.0, 4.0 }, "sqrt(165)")]
        [InlineData("(1+2*x)/((4+4*x+3*x^2)*sqrt(-1+6*x+x^2))", new[] { 0.5, 1.0, 2.0, 3.0, 5.0, -7.0 }, "sqrt(7)")]
        [InlineData("(-2+x)/((17-18*x+5*x^2)*sqrt(13-22*x+10*x^2))", new[] { -2.0, -0.5, 0.3, 1.0, 2.0, 4.0 }, "sqrt(35)")]
        [InlineData("(3+2*x)/((3+2*x+x^2)^2*sqrt(4+2*x+x^2))", new[] { -2.0, -0.5, 0.3, 1.0, 2.0, 4.0 }, "sqrt(2)")]
        public void AQuadraticRadicandWithResiduesInAQuarticField(string integrand, double[] points, string constant)
        {
            DifferentiatesBack(integrand, points);
            Assert.Contains(constant, integrand.ToEntity().Integrate("x").Stringize());
        }

        /// <summary>
        /// And what is elliptic is declined: <c>1/((x - 1) sqrt(x^3 + 1))</c>, where the
        /// radicand is <c>2</c> at the pole and no branch is rational, and
        /// <c>x/((x - 2) sqrt(1 + x^3))</c>, which is <c>1/sqrt(1 + x^3)</c> beside Welz's; and
        /// at <c>x^2 + 1</c>, where <c>x^3 + x^2 + x + 2</c> is <c>1</c>, the line <c>x</c>
        /// has the norm <c>(x + 2)(x^2 + 1)</c>, and <c>x + 2</c> is no place of the integrand.
        /// </summary>
        [Theory]
        [InlineData("1/((x-1)*sqrt(x^3+1))")]
        [InlineData("x/((x-2)*sqrt(1+x^3))")]
        [InlineData("1/((x^2+1)*sqrt(x^3+x^2+x+2))")]
        public void TheEllipticIsDeclined(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.Contains("integral(", integral.Stringize());
        }
    }
}
