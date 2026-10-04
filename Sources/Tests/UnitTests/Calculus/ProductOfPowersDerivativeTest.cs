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
    /// A product of powers times a sum that is the derivative of the product with some of
    /// the powers raised by one, read off the sum: <c>e^x x^2 ln(x)^2 (3 + (3 + x) ln(x))</c>
    /// is <c>(e^x x^3 ln(x)^3)'</c>, for symbolic exponents too.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// The product rule on <c>G = K prod f_i^(e_i)</c> gives <c>G' = G sum e_i f_i'/f_i</c>, so
    /// the sum in such an integrand names the powers to raise; every subset of them is tried,
    /// the quotient of the integrand by the candidate's derivative must be a constant, decided
    /// at sampled points and then read off one monomial, and the answer is differentiated
    /// back. Nothing else reads a symbolic exponent, and splitting the sum loses it, since
    /// <c>x^m ln(x)^n</c> on its own is not elementary.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ProductOfPowersDerivativeTest
    {
        private static readonly double[] Points = { 0.3, 0.9, 1.7, 2.6 };

        /// <summary>
        /// <see cref="DifferentiatesBack"/> with every symbol but <c>x</c> pinned to a distinct
        /// value off the integers, so that a symbolic base or slope is a number on both sides.
        /// </summary>
        private static void DifferentiatesBackWithParametersPinned(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var pinned = 0;
            foreach (var parameter in original.Vars)
            {
                if (parameter.Name == "x")
                    continue;
                var value = new[] { 1.3, 2.1, 0.7, 1.9, 3.1, 0.4 }[pinned++ % 6] + pinned / 6;
                derivative = derivative.Substitute(parameter, value);
                original = original.Substitute(parameter, value);
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
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} of {points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// Rubi's, with symbols for the exponents: <c>x^m ln(x)^n (1 + n + (1 + m) ln x)</c> is
        /// <c>(x^(m + 1) ln(x)^(n + 1))'</c>, and
        /// <c>F^(c (a + b x)) x^m ln(d x)^n (p + p n + p (1 + m + b c x ln F) ln(d x))</c> is
        /// <c>(p F^(c (a + b x)) x^(m + 1) ln(d x)^(n + 1))'</c>, with a power of <c>x</c> below
        /// the bar, or raised to nothing, in the others.
        /// </summary>
        [Theory]
        [InlineData("x^m*ln(x)^n*(1 + n + (1 + m)*ln(x))")]
        [InlineData("F^(c*(a + b*x))*x^m*ln(d*x)^n*(p + p*n + p*(1 + m + b*c*x*ln(F))*ln(d*x))")]
        [InlineData("F^(c*(a + b*x))*ln(d*x)^n*(p + p*n + b*c*p*x*ln(F)*ln(d*x))/x")]
        [InlineData("F^(c*(a + b*x))*ln(d*x)^n*(p + p*n + p*(-2 + b*c*x*ln(F))*ln(d*x))/x^3")]
        public void ASymbolicExponentOnEachPower(string integrand)
            => DifferentiatesBackWithParametersPinned(integrand, Points);

        /// <summary>Whole exponents, which the tower ansätze do not read either: the sum is not elementary apart.</summary>
        [Theory]
        [InlineData("e^x*x^2*ln(x)^2*(3 + (3 + x)*ln(x))")]
        [InlineData("F^(c*(a + b*x))*x^2*ln(d*x)^2*(3 + (3 + b*c*x*ln(F))*ln(d*x))")]
        public void AWholeExponentOnEachPower(string integrand)
            => DifferentiatesBackWithParametersPinned(integrand, Points);

        /// <summary>
        /// The constant in front is read off the answer, not assumed: <c>p</c> above, and here
        /// a symbol that does not cancel.
        /// </summary>
        [Fact]
        public void TheConstantIsRead()
            => Assert.Equal("p * x ^ (m + 1) * ln(x) ^ (n + 1) + C",
                "p*x^m*ln(x)^n*(1 + n + (1 + m)*ln(x))".ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// A sum that is not the derivative's is declined, not answered: <c>x^m ln(x)^n (1 + ln x)</c>
        /// with symbols for <c>m</c> and <c>n</c> is not elementary.
        /// </summary>
        [Fact]
        public void WhatIsNotADerivativeIsDeclined()
            => Assert.Contains("integral(", "x^m*ln(x)^n*(1 + ln(x))".ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// Rubi's 1.3.2, two symbolic powers of a quadratic and a cubic beside a power of x:
        /// the sum is read as a polynomial and the constant off one monomial. It was a timeout.
        /// </summary>
        [Fact]
        public void TwoSymbolicPowersOfPolynomials()
            => DifferentiatesBackWithParametersPinned(
                "x*(a+b*x+c*x^2)^m*(d+h*x+f*x^2+g*x^3)^n*(2*a*d+(3*b*d+3*a*h+b*d*m+a*h*n)*x+(4*c*d+4*b*h+4*a*f+2*c*d*m+b*h*m+b*h*n+2*a*f*n)*x^2+(5*c*h+5*b*f+5*a*g+2*c*h*m+b*f*m+c*h*n+2*b*f*n+3*a*g*n)*x^3+(6*c*f+6*b*g+2*c*f*m+b*g*m+2*c*f*n+3*b*g*n)*x^4+c*g*(7+2*m+3*n)*x^5)",
                Points);

        /// <summary>
        /// Where a power is not a whole one, a product with no sum beside it, Rubi's 1.1.4.2, and
        /// x raised from a power the integrand does not have, Rubi's 1.2.3.5.
        /// </summary>
        [Theory]
        [InlineData("1/(x^2*sqrt(a*x+b*x^4))")]
        [InlineData("(a+b*x^n+c*x^(2*n))^p*(a+b*(1+n+n*p)*x^n+c*(1+2*n*(1+p))*x^(2*n))")]
        public void WithoutASumOrWithXRaisedFromNothing(string integrand)
            => DifferentiatesBackWithParametersPinned(integrand, Points);

        /// <summary>A product of powers that is no such derivative is still declined: <c>sqrt(1 + x^3)</c> is elliptic.</summary>
        [Fact]
        public void APowerThatIsNoDerivativeIsDeclined()
            => Assert.Contains("integral(", "sqrt(1 + x^3)".ToEntity().Integrate("x").Stringize());
    }
}
