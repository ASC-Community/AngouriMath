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
    /// A half-odd power of the secant beside the cosine, or of the cosecant beside the sine,
    /// which is the cosine's power on the other side of the bar times <c>sec(x)^p cos(x)^p</c>,
    /// 1 where the cosine is positive and -1 where it is negative. Compared on both sides, where
    /// the integrand is real and where it is not, which is where the constant is what keeps the
    /// answer right. Rubi's 4.2.2.1, 4.2.3.1 and 4.2.4.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfTheSecantBesideTheCosineIntegralTest
    {
        [Theory]
        [InlineData("sec(x)^(3/2)/sqrt(1 + cos(x))")]
        [InlineData("sec(x)^(5/2)*(a + a*cos(x))^(3/2)")]
        [InlineData("1/((a + a*cos(x))^(7/2)*sec(x)^(7/2))")]
        [InlineData("(a + a*cos(x))^(3/2)*(A + B*cos(x))/sqrt(sec(x))")]
        [InlineData("(A + c*cos(x)^2)*sec(x)^(11/2)*sqrt(a + a*cos(x))")]
        [InlineData("sqrt(csc(x))*sqrt(a + a*sin(x))")]
        [InlineData("1/((a + a*sin(x))^(3/2)*csc(x)^(3/2))")]
        public void OnBothSidesOfTheCosine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("A", 0.9).Substitute("B", 1.4).Substitute("c", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -2.6, -1.1, -0.4, 0.3, 0.8, 1.2, 2.0, 2.7 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 6, $"only {compared} points could be compared for {integrand}");
        }
    }
}
