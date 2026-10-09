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
    /// A half-odd power of <c>a ± a sec(y)</c> beside one of <c>c + d sec(y)</c>, in the half
    /// angle's sine, for a symbolic slope and shift too. Rubi's 4.5.2.1 and 4.5.2.3.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TwoRootsOfSecantSumsIntegralTest
    {
        [Theory]
        [InlineData("sec(g + f*x)*sqrt(a + a*sec(g + f*x))/sqrt(c + d*sec(g + f*x))")]
        [InlineData("sqrt(c + d*sec(g + f*x))/sqrt(a + a*sec(g + f*x))")]
        [InlineData("sqrt(a + a*sec(g + f*x))/(c + d*sec(g + f*x))^(3/2)")]
        [InlineData("sqrt(a - a*sec(x))/sqrt(c + d*sec(x))")]
        [InlineData("csc(x)*sqrt(a + a*csc(x))/sqrt(c + d*csc(x))")]
        public void InTheHalfAngleSine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 5000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 0.9).Substitute("d", 0.4).Substitute("g", 0.2).Substitute("f", 0.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.2, -0.7, 0.3, 0.8, 1.3, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
