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
    /// An algebraic integrand with a linear <c>c + d x</c> under a power inside a sum, written in
    /// <c>u = c + d x</c>, as Rubi's 1.1.3.2, 1.2.3.2 and 1.3.1 write whole sections of it.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back at points where the integrand is real, never against a
    /// printed form.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class OneShiftedLinearIntegralTest
    {
        private static void DifferentiatesBack(string integrand, double[] points, params (string, double)[] pins)
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

        /// <summary>
        /// A binomial and a trinomial in the linear, with the variable outside it as a power of
        /// x, as a power of the linear, or as a multiple of the linear: each declined, or ran past
        /// twenty seconds, written in x.
        /// </summary>
        [Theory]
        [InlineData("x^3/(a + b*(c + d*x)^3)")]
        [InlineData("(c + d*x)^4/(a + b*(c + d*x)^3)")]
        [InlineData("1/((c + d*x)^4*(a + b*(c + d*x)^3))")]
        [InlineData("(c*m + d*m*x)^3/(a + b*(c + d*x)^3)^2")]
        [InlineData("(c + d*x)^4/(a + b*(c + d*x)^2 + k*(c + d*x)^4)^2")]
        public void WrittenInTheLinear(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.3, 0.9, 1.7, 2.6 },
                ("a", 1.3), ("b", 0.7), ("c", 0.4), ("d", 1.1), ("k", 0.6), ("m", 2.3));
    }
}
