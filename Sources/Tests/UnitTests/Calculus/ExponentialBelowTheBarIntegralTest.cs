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
    /// Exponentials of one linear below the bar, beside a power of a linear: written in the
    /// exponential the others are whole powers of, they are a sum of whole powers of it, and
    /// each term over the linear is an exponential integral. <c>1/(x e^(2x))</c> was declined
    /// where <c>e^(-2x)/x</c> was answered, and so was <c>1/((c + d x)(a + a tanh(k + f x)))</c>,
    /// Rubi's 6.3.1, which is <c>(1 + e^(-2(k + f x)))/(2 a (c + d x))</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialBelowTheBarIntegralTest
    {
        [Theory]
        [InlineData("1/(x*exp(2*x))")]
        [InlineData("(exp(2*x) + 1)/(2*x*exp(2*x))")]
        [InlineData("1/(x*(1 + tanh(x)))")]
        [InlineData("1/((c + d*x)*(a + a*tanh(k + f*x)))")]
        [InlineData("1/((c + d*x)^2*(a + a*tanh(k + f*x))^3)")]
        [InlineData("1/((c + d*x)*(a + a*coth(k + f*x))^2)")]
        [InlineData("1/((c + d*x)*(a - a*tanh(e + f*x)))")]
        [InlineData("1/(x*(x + 1)*exp(x))")]
        public void ExpandedOverTheLinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("c", 1.3).Substitute("d", 1.7).Substitute("f", 0.9).Substitute("k", 0.4);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.7, -0.9, 0.3, 0.8, 1.6, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
