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
    /// A polynomial over a power of a trinomial <c>a + b x^n + c x^(2n)</c> with symbols in it,
    /// taken down a power at a time by its recurrence to the trinomial itself.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Rubi's 1.2.2.2 <c>x^4/(a + b x^2 + c x^4)^3</c> and the like: the Hermite reduction answers
    /// the square, and its system grew past what it takes at the cube. Differentiated back on
    /// both sides of <c>4 a c - b^2</c>, since the roots the last step writes are complex for one.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfATrinomialIntegralTest
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

        /// <summary>The cube of a trinomial in x^2, and the square of one in x^3.</summary>
        [Theory]
        [InlineData("x^4/(a + b*x^2 + c*x^4)^3", 0.7)]
        [InlineData("1/(a + b*x^2 + c*x^4)^3", 0.7)]
        [InlineData("(d + k*x^2)/(a + b*x^2 + c*x^4)^3", 0.7)]
        [InlineData("x/(a + b*x^3 + c*x^6)^2", 0.7)]
        [InlineData("x^4/(a + b*x^2 + c*x^4)^3", 3.1)]
        [InlineData("(d + k*x^2)/(a + b*x^2 + c*x^4)^3", 3.1)]
        public void PastTheSquare(string integrand, double b)
            => DifferentiatesBack(integrand, ("a", 1.3), ("b", b), ("c", 0.9), ("d", 0.4), ("k", 1.1));
    }
}
