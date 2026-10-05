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
    /// <c>d + k x + f sqrt(Q)</c> with <c>k^2</c> the leading coefficient of <c>Q</c> times
    /// <c>f^2</c> is Euler's variable, and its powers are powers of the variable: Rubi's 1.3.2
    /// <c>(d + e x + f sqrt(a + b x + e^2 x^2/f^2))^n</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.6</c>, <c>d = 0.7</c>,
    /// <c>f = 1.1</c>, <c>k = 0.9</c> and <c>n = 0.37</c>, where every root is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SumOfALinearAndARootAsEulersVariableIntegralTest
    {
        [Theory]
        // A symbol for the leading coefficient, under a whole power.
        [InlineData("1/(d + k*x + f*sqrt(a + k^2*x^2/f^2))")]
        // A root of the sum, the constants of the substitution named while it is asked.
        [InlineData("sqrt(d + k*x + f*sqrt(a + b*x + k^2*x^2/f^2))")]
        [InlineData("1/(d + k*x + f*sqrt(a + b*x + k^2*x^2/f^2))^(3/2)")]
        // A symbolic power, term by term by the power rule.
        [InlineData("(d + k*x + f*sqrt(a + 2*d*k*x/f^2 + k^2*x^2/f^2))^n")]
        // And beside powers of the radicand, whole and not, before the split expands them.
        [InlineData("(a + 2*d*k*x/f^2 + k^2*x^2/f^2)^2*(d + k*x + f*sqrt(a + 2*d*k*x/f^2 + k^2*x^2/f^2))^n")]
        [InlineData("(a + 2*d*k*x/f^2 + k^2*x^2/f^2)^(3/2)*(d + k*x + f*sqrt(a + 2*d*k*x/f^2 + k^2*x^2/f^2))^n")]
        public void IsIntegratedInTheSum(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.6).Substitute("d", 0.7)
                .Substitute("f", 1.1).Substitute("k", 0.9).Substitute("n", 0.37);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.5, -1.2, 0.3, 0.8, 1.4 })
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
