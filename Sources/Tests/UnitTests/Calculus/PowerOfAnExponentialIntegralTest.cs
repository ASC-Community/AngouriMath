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
    /// A power of an exponential beside a polynomial below the bar: <c>(F^(g (k + f x)))^n</c> has
    /// the constant logarithmic derivative <c>n g f ln F</c>, so it is a constant times
    /// <c>e^(n g f ln(F) x)</c> wherever it is differentiable, and over a linear it is an
    /// exponential integral. Rubi's 2.2, <c>(a + b (F^(g (e + f x)))^n)^p/(c + d x)^m</c>, all
    /// declined before.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfAnExponentialIntegralTest
    {
        [Theory]
        [InlineData("(F^x)^2/x")]
        [InlineData("(F^x)^n/x")]
        [InlineData("(exp(x))^n/x")]
        [InlineData("(a + b*(F^(g*(k + f*x)))^n)/(c + d*x)")]
        [InlineData("(a + b*(F^(g*(k + f*x)))^n)^2/(c + d*x)^2")]
        [InlineData("(a + b*(F^(g*(k + f*x)))^n)^3/(c + d*x)^3")]
        public void OverALinear(string integrand) => DifferentiatesBack(integrand, 3, realOnly: true);

        /// <summary>
        /// With a negative base, <c>(F^x)^n</c> is not <c>F^(n x)</c>: the two differ by a power of
        /// <c>e^(2 pi i n)</c> that changes where <c>F^x</c> crosses the negative axis. The answer
        /// holds between those crossings, compared as complex numbers.
        /// </summary>
        [Theory]
        [InlineData("(F^x)^n/x")]
        [InlineData("(a + b*(F^(g*(k + f*x)))^n)/(c + d*x)")]
        public void OverALinearWithANegativeBase(string integrand) => DifferentiatesBack(integrand, -3, realOnly: false);

        private static void DifferentiatesBack(string integrand, double @base, bool realOnly)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3).Substitute("d", 1.7)
                .Substitute("f", 0.9).Substitute("g", 1.2).Substitute("k", 0.4).Substitute("n", 2.5).Substitute("F", @base);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.7, -0.9, 0.3, 0.8, 1.6, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || realOnly && Math.Abs((double)want.ImaginaryPart) > 1e-12)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
