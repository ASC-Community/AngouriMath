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
    /// An exponential of a hyperbolic sine or cosine of a linear beside a function of it over its
    /// derivative, under <c>u</c> twice that sine or cosine: <c>e^(n cosh(a + b x)) tanh(a + b x)</c>
    /// is <c>Ei(n cosh(a + b x))/b</c>, as <c>e^(n cos(x)) tan(x)</c> is <c>-Ei(n cos(x))</c>, and was
    /// declined, the hyperbolic functions arriving as exponentials. Rubi's 6.7.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialOfAHyperbolicIntegralTest
    {
        [Theory]
        [InlineData("exp(n*cosh(a + b*x))*tanh(a + b*x)")]
        [InlineData("exp(n*sinh(a + b*x))*coth(a + b*x)")]
        [InlineData("exp(n*sinh(a + b*x))*sinh(2*(a + b*x))")]
        [InlineData("exp(n*cosh(1/2*(a + b*x)))*sinh(a + b*x)")]
        [InlineData("exp(n*sinh(a*c + b*c*x))*cosh(c*(a + b*x))")]
        public void UnderTwiceTheSineOrCosine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.3).Substitute("b", 0.7).Substitute("c", 1.1).Substitute("n", 0.6);
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
