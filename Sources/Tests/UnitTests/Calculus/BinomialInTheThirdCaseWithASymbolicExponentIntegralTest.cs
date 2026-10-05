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
    /// A binomial differential <c>x^m (a + b x^n)^p</c> with a symbol in its exponents, where
    /// <c>(m + 1)/n + p + 1</c> is a whole number at most 0: Rubi's 1.1.3.2:3301-3304 and the same
    /// with a power of x beside the binomial.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.7</c>, <c>n = 1.7</c> and
    /// <c>m = 0.6</c>, at positive <c>x</c>, where a symbolic power of it is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class BinomialInTheThirdCaseWithASymbolicExponentIntegralTest
    {
        [Theory]
        [InlineData("1/(a + b*x^n)^((1 + n)/n)")]
        [InlineData("1/(a + b*x^n)^((1 + 2*n)/n)")]
        [InlineData("1/(a + b*x^n)^((1 + 4*n)/n)")]
        [InlineData("x^2/(a + b*x^n)^((3 + 2*n)/n)")]
        [InlineData("x^m*(a + b*x^n)^(-(m + 1)/n - 3)")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("n", 1.7).Substitute("m", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.4, 2.1 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
