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
    /// A sine or cosine of <c>a + b/L^k</c> beside <c>L^m</c>, <c>m &lt;= -3</c>, under <c>u = 1/L</c>:
    /// <c>sin(a + b/x)/x^3</c> is <c>-u sin(a + b u)</c>, elementary by parts, and was declined.
    /// Rubi's 4.1.12 and 4.2.12, <c>(e x)^m (a + b sin(c + d x^n))^p</c> for a negative <c>n</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class FunctionOfTheReciprocalIntegralTest
    {
        [Theory]
        [InlineData("sin(a + b/x)/x^3")]
        [InlineData("cos(a + b/x)/x^4")]
        [InlineData("sin(a + b/x)^2/x^3")]
        [InlineData("cos(a + b/x)^3/x^4")]
        [InlineData("sin(a + b/x^2)/x^5")]
        [InlineData("sin(a + b/(c + d*x))/(c + d*x)^3")]
        public void UnderTheReciprocal(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3).Substitute("d", 1.7);
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
