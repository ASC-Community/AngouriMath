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
    /// A polynomial over a power of a binomial <c>a + b x^n</c>, <c>n &gt;= 3</c>, with symbols in
    /// it, taken down a power at a time by the classical recurrence to the binomial itself.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Rubi's 1.1.3.8 <c>(c + d x)/(a + b x^4)^4</c> and the like: the Hermite reduction answers
    /// the square and the cube of the binomial, and its system grew past what it takes from the
    /// fourth power on. Differentiated back with <c>a</c> of either sign, since the roots the
    /// last step writes are complex for one of them.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfABinomialIntegralTest
    {
        private static readonly double[] Points = { 0.3, 0.9, 1.7, 2.6 };

        private static void DifferentiatesBack(string integrand, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            Entity original = integrand.ToEntity();
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points could be compared for {integrand}");
        }

        /// <summary>The fourth power and past it, of binomials of the third, fourth and fifth degree.</summary>
        [Theory]
        [InlineData("1/(a + b*x^4)^4", 1.3)]
        [InlineData("(c + d*x)/(a + b*x^4)^4", 1.3)]
        [InlineData("(c + d*x)/(a + b*x^3)^4", 1.3)]
        [InlineData("(c + d*x + e*x^2 + f*x^3 + g*x^4 + h*x^5 + k*x^6)/(a + b*x^4)^4", 1.3)]
        [InlineData("x^2/(a + b*x^5)^5", 1.3)]
        [InlineData("(c + d*x)/(a + b*x^4)^4", -1.3)]
        [InlineData("(c + d*x)/(a + b*x^3)^4", -1.3)]
        public void PastTheCube(string integrand, double a)
            => DifferentiatesBack(integrand, ("a", a), ("b", 0.7), ("c", 0.4), ("d", 1.1), ("e", 0.6),
                ("f", 1.7), ("g", 0.9), ("h", 2.1), ("k", 0.3));
    }
}
