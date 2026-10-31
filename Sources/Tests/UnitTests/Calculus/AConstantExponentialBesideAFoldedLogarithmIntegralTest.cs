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
    /// A hyperbolic function of <c>a + b ln(x)</c> folds to a rational function of <c>x</c> with
    /// <c>e^a</c> among its coefficients, and that exponential is also asked as a symbol and
    /// written back, the shorter answer kept. As an exponential its arithmetic did not cancel:
    /// <c>tanh(a + 2 ln(x))^2/x^3</c> came back a piecewise of 7,796 characters on the signs of
    /// <c>4 e^(2 a)</c> and <c>4 e^(-2 a)</c>. Rubi's 6.3.2 and 6.4.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/233">#233</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AConstantExponentialBesideAFoldedLogarithmIntegralTest
    {
        private static readonly double[] Points = { 0.3, 0.6, 0.9, 1.4, 2.1 };

        private static void DifferentiatesBack(string integrand, int longest)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length <= longest, $"{text.Length} characters of answer for {integrand}");
            foreach (var a in new[] { -0.7, 0.6, 1.3 })
            {
                Entity Pinned(Entity e) => e.Substitute("a", a);
                var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
                var original = Pinned(integrand.ToEntity());
                var compared = 0;
                foreach (var at in Points)
                {
                    var want = original.Substitute("x", at).EvalNumerical();
                    var got = derivative.Substitute("x", at).EvalNumerical();
                    if (want.IsNaN)
                        continue;
                    compared++;
                    Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                                < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                        $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, a = {a}, where the integrand is {want}");
                }
                Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand} at a = {a}");
            }
        }

        /// <summary>
        /// An odd power of <c>x</c>, a function of <c>x^2</c> under <c>t = x^2</c>: in a symbol
        /// these are a few hundred characters at most, and as exponentials up to thousands.
        /// </summary>
        [Theory]
        [InlineData("tanh(a + 2*ln(x))^2/x^3")]
        [InlineData("coth(a + 2*ln(x))^2/x^3")]
        [InlineData("x^3*tanh(a + 2*ln(x))^2")]
        [InlineData("x^3*coth(a + 2*ln(x))^2")]
        [InlineData("x*tanh(a + 2*ln(x))^2")]
        [InlineData("tanh(a + 2*ln(x))^2/x")]
        [InlineData("coth(a + 2*ln(x))^2/x")]
        [InlineData("tanh(a + 2*ln(x))/x^3")]
        [InlineData("coth(a + 2*ln(x))/x^3")]
        [InlineData("x^3*tanh(a + 2*ln(x))")]
        [InlineData("x^3*coth(a + 2*ln(x))")]
        [InlineData("x*tanh(a + 2*ln(x))")]
        [InlineData("x*coth(a + 2*ln(x))")]
        public void AnOddPowerOfX(string integrand) => DifferentiatesBack(integrand, 400);

        /// <summary>
        /// An even power of <c>x</c>, where the factorisation reads a symbol and splits the
        /// biquadratic over it into arms on its sign -- <c>coth(a + 2 ln(x))/x^2</c> is 3,577
        /// characters in the symbol and 413 as the exponential, which is kept. The bound was 1,300
        /// until the arctangent's poles joined the points the answer lists as having no value:
        /// <c>x^2 coth(a + 2 ln(x))^2</c> keeps two arctangents and went from 1,128 characters to
        /// 1,382 with their two conditions. https://github.com/asc-community/AngouriMath/issues/2256
        /// </summary>
        [Theory]
        [InlineData("coth(a + 2*ln(x))/x^2")]
        [InlineData("coth(a + 2*ln(x))^2/x^2")]
        [InlineData("coth(a + 2*ln(x))^2")]
        [InlineData("x^2*coth(a + 2*ln(x))^2")]
        [InlineData("tanh(a + 2*ln(x))^2")]
        public void AnEvenPowerOfX(string integrand) => DifferentiatesBack(integrand, 1400);
    }
}
