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
    /// Fractional powers of two different linears in the variable, rationalised by the
    /// quotient of their roots, <c>t = (a x + b)^(1/q) / (c x + d)^(1/q)</c>; and a root of a
    /// product of powers of linears -- Timofeev's spelling, <c>((x - 1)^4 (x + 1)^2)^(1/3)</c>
    /// -- written apart into those powers where that holds on the reals.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back at points where the integrand is real, never against a
    /// printed form. The answers are rational functions and logarithms of the quotient of the
    /// two roots, whose shape says nothing about whether they differentiate back.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class TwoLinearRadicalsIntegralTest
    {
        private static void DifferentiatesBack(string integrand, params double[] points)
            => DifferentiatesBackPinned(integrand, points);

        private static void DifferentiatesBackPinned(string integrand, double[] points, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());

            // The integration constant is C_1 where the integrand has a coefficient named C,
            // and zeroing C there would take the coefficient out of the answer.
            Entity original = integrand.ToEntity();
            var derivative = (original.Vars.Any(v => v.Name == "C") ? integral : integral.Substitute("C", 0)).Differentiate("x");
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points could be compared for {integrand}");
        }

        /// <summary>Two linears under roots of the same order, above and below 1 where both roots are real.</summary>
        [Theory]
        [InlineData("(x - 1)^(-4/3) * (x + 1)^(-2/3)")]
        [InlineData("(x + 2)^(1/3) / (x - 1)^(4/3)")]
        [InlineData("1/(x * (x - 1)^(1/3) * (x + 1)^(2/3))")]
        [InlineData("(x - 1)^(2/3) * (x + 1)^(1/3)")]
        public void TwoRootsOfTheSameOrder(string integrand)
            => DifferentiatesBack(integrand, 1.3, 1.7, 2.4, 3.1, -1.5, -2.7);

        /// <summary>
        /// Two square roots of linears with symbolic coefficients, Hearn's
        /// <c>sqrt(a + b x) sqrt(c + d x)</c>. Beside a polynomial they are answered by
        /// undetermined coefficients. With a root below the bar to a higher power, the last
        /// row, they go through the quotient of the roots: the terms are read as written so
        /// that the power of the quadratic the derivative of <c>x(t)</c> carries stays that
        /// power, and the power of a quadratic with a symbolic leading coefficient is made
        /// monic before the rational rules see it.
        /// </summary>
        [Theory]
        [InlineData("sqrt(a + b*x)*sqrt(c + d*x)")]
        [InlineData("x/(sqrt(a + b*x)*sqrt(c + d*x))")]
        [InlineData("1/(sqrt(a + b*x)*sqrt(c + d*x))")]
        [InlineData("sqrt(c + d*x)/(a + b*x)^(3/2)")]
        // A power of x beside them: each term of the rational function in t is split on the
        // sign of the same quantity, and their sum combines sign by sign rather than pair by pair
        // (https://github.com/asc-community/AngouriMath/issues/718).
        [InlineData("x*sqrt(a + b*x)*sqrt(c + d*x)")]
        [InlineData("x^2*sqrt(a + b*x)*sqrt(c + d*x)")]
        public void SymbolicCoefficients(string integrand)
            => DifferentiatesBackPinned(integrand, new[] { 0.3, 0.9, 1.7, 2.6 }, ("a", 1.7), ("b", 2.3), ("c", 0.6), ("d", 1.1));

        /// <summary>
        /// A polynomial beside the square roots of two linears with symbols in them, Rubi's
        /// 1.1.1.6, by undetermined coefficients: <c>U sqrt(L1) sqrt(L2)</c> and a multiple of
        /// the integral of <c>1/(sqrt(L1) sqrt(L2))</c>. Through the quotient of the roots
        /// the first five took more than 4 GB each. With the slopes of opposite signs the
        /// integral left is an arctangent, and the sixth row has numbers for the slopes, so the
        /// arctangent is written as one.
        /// </summary>
        [Theory]
        [InlineData("(A + B*x + C*x^2)*sqrt(c + d*x)*sqrt(e + f*x)")]
        [InlineData("(a + b*x)^2*(A + B*x + C*x^2)*sqrt(c + d*x)*sqrt(e + f*x)")]
        [InlineData("(A + B*x + C*x^2)*sqrt(c + d*x)/sqrt(e + f*x)")]
        [InlineData("(e + f*x)^3*(A + B*x + C*x^2)*sqrt(a + b*x)*sqrt(a*c - b*c*x)")]
        [InlineData("(c + d*x)^(3/2)*sqrt(e + f*x)")]
        [InlineData("x^2*sqrt(a + 2*x)*sqrt(c - 3*x)")]
        [InlineData("(a + b*x)*(A + B*x + C*x^2)/(sqrt(c + d*x)*sqrt(e + f*x))")]
        [InlineData("x*sqrt(1 + d*x)*sqrt(1 - f*x)")]
        public void APolynomialBesideTheRootsOfTwoLinears(string integrand)
            => DifferentiatesBackPinned(integrand, new[] { 0.3, 0.9, 1.7, 2.6 },
                ("a", 1.3), ("b", 0.7), ("c", 1.9), ("d", 0.45), ("e", 2.3), ("f", 0.55), ("A", 1.1), ("B", -0.8), ("C", 0.35));

        /// <summary>
        /// The same over a power of a third linear, the rest of Rubi's 1.1.1.6: the terms over
        /// its powers are reduced to the integral of <c>1/((g + h x) S)</c>, an arctangent of the
        /// quotient of the roots, which the substitution answered by a rational function in it
        /// past the corpus's budget or not at all.
        /// </summary>
        [Theory]
        [InlineData("1/((1 + x)*sqrt(a + b*x)*sqrt(c - d*x))")]
        [InlineData("(A + B*x + C*x^2)/((e + f*x)^2*sqrt(a + b*x)*sqrt(a*c - b*c*x))")]
        [InlineData("(A + B*x + C*x^2)*sqrt(c + d*x)*sqrt(e + f*x)/(a + b*x)^3")]
        [InlineData("(A + B*x + C*x^2)*sqrt(c + d*x)/((a + b*x)^4*sqrt(e + f*x))")]
        public void OverAPowerOfAThirdLinear(string integrand)
            => DifferentiatesBackPinned(integrand, new[] { 0.3, 0.9, 1.7, 2.6 },
                ("a", 1.3), ("b", 0.7), ("c", 1.9), ("d", 0.45), ("e", 2.3), ("f", 0.55), ("A", 1.1), ("B", -0.8), ("C", 0.35));

        /// <summary>Roots of different orders sharing a common one: Timofeev's 315, a sixth root of the quotient.</summary>
        [Fact]
        public void RootsOfDifferentOrdersInASum()
            => DifferentiatesBack("x*(1+x)^(2/3)*sqrt(1-x)/(-(1-x)^(5/6)*(1+x)^(1/3)+(1-x)^(2/3)*sqrt(1+x))", 0.31, 0.57, 0.83, -0.2, -0.6);

        /// <summary>
        /// The root of a product of powers of linears, written apart: an odd root exactly, by
        /// the real root a negative number has here; an even one where the phases agree on
        /// every real interval. Timofeev's 318, 319 and 320.
        /// </summary>
        [Theory]
        [InlineData("1/((-1+x)^4*(1+x)^2)^(1/3)", new[] { 0.31, 0.57, 0.83, 1.29, 1.77, -1.5, -2.7 })]
        [InlineData("1/((-1+x)^7*(1+x)^2)^(1/3)", new[] { 0.31, 0.57, 0.83, 1.29, 1.77, -1.5, -2.7 })]
        [InlineData("1/((-1+x)^3*(2+x)^5)^(1/4)", new[] { 1.29, 1.77, 2.41, -2.5, -3.2 })]
        public void ARootOfAProductOfPowersOfLinears(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// A root of a polynomial whose linear factors already stand under roots of their own
        /// elsewhere: <c>(1 - x^2)^(1/4)</c> beside <c>sqrt(1 - x)</c> and <c>sqrt(1 + x)</c>,
        /// Timofeev's 314, is <c>((1 - x)(1 + x))^(1/4)</c> and comes apart on <c>(-1, 1)</c>;
        /// a root of a quadratic on its own is left as written, Euler's.
        /// </summary>
        [Fact]
        public void AFactoredRootBesideItsFactorsRoots()
            => DifferentiatesBack("x^2*(1-x^2)^(1/4)*sqrt(1+x)/(sqrt(1-x)*(sqrt(1-x)-sqrt(1+x)))", 0.31, 0.57, 0.83, -0.2, -0.6);

        /// <summary>
        /// Two square roots of linears with one slope, rationalised together by their sum
        /// <c>v = sqrt(L1) + sqrt(L2)</c>, whose difference is <c>(L1 - L2)/v</c>: an inverse
        /// function of the difference is one of <c>1/v</c> beside a rational function of v, which
        /// parts closes. Charlwood's arcsine was declined after five seconds, and Rubi's 5.3.7
        /// has the arctangent's powers of x.
        /// </summary>
        [Theory]
        [InlineData("asin(sqrt(1 + x) - sqrt(x))")]
        [InlineData("acos(sqrt(1 + x) - sqrt(x))")]
        [InlineData("x^3*atan(-sqrt(x)+sqrt(1+x))")]
        public void AnInverseFunctionOfTheDifferenceOfTwoRootsOfOneSlope(string integrand)
            => DifferentiatesBack(integrand, 0.3, 0.7, 1.1, 1.4);

        /// <summary>
        /// Two such roots where they stand in no inverse function or logarithm are the radical
        /// rules', and stay theirs: taken under v first, each of these ran out the
        /// corpus's budget, where they are answered in under 40 ms. Rubi's 1.3.2, and Timofeev's
        /// 127, a quotient of the two roots under the arccosine.
        /// </summary>
        [Theory]
        [InlineData("x^2/(sqrt(a + b*x) + sqrt(c + b*x))")]
        [InlineData("acos(sqrt(x/(1 + x)))")]
        public void WhatTheOtherRulesAnswerStaysTheirs(string integrand)
            => DifferentiatesBackPinned(integrand, new[] { 0.3, 0.7, 1.1, 1.4 }, ("a", 2), ("b", 1.5), ("c", 0.7));

        /// <summary>
        /// And a product of the two roots, Rubi's 1.2.1.4: answered through their quotient in a
        /// quarter of a second, and past the corpus's budget when taken under v first.
        /// </summary>
        [Fact]
        public void AProductOfTwoRootsOfOneSlopeStaysTheirs()
            => DifferentiatesBack("sqrt(-1 + x)*sqrt(1 + x)/(1 + x - x^2)", 1.3, 1.7, 2.4, 3.1);
    }
}
