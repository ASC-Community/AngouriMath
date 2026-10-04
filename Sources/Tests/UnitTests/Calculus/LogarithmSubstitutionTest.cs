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
    /// An integrand that is a function of <c>ln(x)</c> and of nothing else, under
    /// <c>u = ln(x)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>sin(ln(x))</c> had no antiderivative and is one substitution away from one that does:
    /// it becomes <c>e^u sin(u)</c>, the cyclic by-parts integral, which
    /// <a href="https://github.com/asc-community/AngouriMath/pull/1278">#1278</a> closes.
    /// </para>
    /// <para>
    /// The substitution is an <b>inverse</b> one — <c>x = e^u</c>, so it introduces an
    /// exponential rather than cancelling one — which is why the general substitution, which
    /// divides by <c>du/dx</c> and asks what is left, does not find it.
    /// </para>
    /// <para>
    /// The sample points are positive, which is where an integrand built from <c>ln(x)</c> is
    /// real: <c>u = ln(x)</c> is a bijection from the positive reals onto the whole line, so the
    /// answer holds wherever the integrand does and nothing further is assumed. A radical
    /// function of whole powers of <c>x</c> is real on both sides of zero, and is compared on
    /// both.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class LogarithmSubstitutionTest
    {
        private static readonly double[] Points = { 0.4, 1.3, 2.7, 5.1 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            Assert.DoesNotContain("integral(", integral.Stringize());

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
                $"only {compared} of {Points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>
        /// A trigonometric function of the logarithm, which lands on the cyclic by-parts integral
        /// and is the case this rule was written for.
        /// </summary>
        [Theory]
        [InlineData("sin(ln(x))")]
        [InlineData("cos(ln(x))")]
        [InlineData("sin(2*ln(x))")]
        [InlineData("sin(ln(x))^2")]
        [InlineData("cos(ln(x))*sin(ln(x))")]
        [InlineData("sin(ln(x)) + cos(ln(x))")]
        public void ATrigonometricFunctionOfTheLogarithm(string integrand)
            => DifferentiatesBack(integrand);

        /// <summary>
        /// Powers of the logarithm, which land on a polynomial times an exponential. The square
        /// already came out through repeated by parts; the cube did not.
        /// </summary>
        [Theory]
        [InlineData("ln(x)^2")]
        [InlineData("ln(x)^3")]
        [InlineData("ln(x)^4")]
        [InlineData("ln(x)^2 + ln(x)")]
        public void APowerOfTheLogarithm(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A logarithm to another base, which is <c>ln(x)/ln(b)</c> and must not be declined for
        /// the spelling — the defect this file's neighbours have carried repeatedly.
        /// </summary>
        [Theory]
        [InlineData("log(2, x)")]
        [InlineData("sin(log(2, x))")]
        [InlineData("log(10, x)^2")]
        public void ALogarithmToAnotherBase(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The neighbours: what already came out through by parts or the general substitution and
        /// must keep coming out.
        /// </summary>
        [Theory]
        [InlineData("ln(x)")]
        [InlineData("ln(x)/x")]
        [InlineData("1/(x*ln(x))")]
        [InlineData("x*ln(x)")]
        [InlineData("x^2*ln(x)")]
        [InlineData("ln(x)/x^2")]
        [InlineData("ln(x) + x")]
        public void TheNeighboursAreUntouched(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A radical function of whole powers of <c>x</c> written through its logarithm, which is
        /// real on both sides of zero: <c>sinh(2 ln(x))</c> is <c>(x^2 - x^(-2))/2</c> for a
        /// negative <c>x</c> as well. Each answer is compared on both sides of zero and on both
        /// sides of 1, where <c>sinh(2 ln(x))</c> changes sign, and holds at every point: the
        /// exponentials of the logarithm are folded into powers of <c>x</c>, and the answer is in
        /// those. Rubi's 6.5.3, 226, and 6.6.3, 188.
        /// </summary>
        [Theory]
        [InlineData("1/csch(2*ln(x))^(1/2)")]
        [InlineData("csch(2*ln(x))^(-3/2)")]
        [InlineData("1/csch(2*ln(13/10*x))^(1/2)")]
        [InlineData("1/sech(2*ln(x))^(1/2)")]
        [InlineData("sqrt(sinh(2*ln(x)))")]
        [InlineData("sqrt(cosh(2*ln(13/10*x)))")]
        public void ARadicalFunctionOfAWholePowerOfX(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { -2.7, -1.9, -1.3, -0.7, -0.4, 0.4, 0.7, 1.3, 1.9, 2.7 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference / Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)) < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// What the read must refuse: an <c>x</c> that survives the rewrite, so the integrand is
        /// not a function of the logarithm alone. <c>ln(x)*sin(x)</c> is the plain case, and
        /// <c>ln(x)/x</c> is the one that shows the test is on what <b>survives</b> rather than on
        /// what appears — it holds a bare <c>x</c> and is a function of the logarithm all the same,
        /// which the general substitution answers and this need not.
        /// </summary>
        [Theory]
        [InlineData("ln(x)*sin(x)")]
        [InlineData("ln(x) + sin(x)")]
        [InlineData("ln(x)*e^x")]
        public void WhatTheReadRefuses(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;   // unanswered is a legitimate verdict; a wrong answer is not
            DifferentiatesBack(integrand);
        }
    }
}
