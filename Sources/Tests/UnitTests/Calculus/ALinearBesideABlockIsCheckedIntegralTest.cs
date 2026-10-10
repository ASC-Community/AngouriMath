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
    /// A power of a linear beside a block with symbols in it is answered only where the answer
    /// differentiates back to the integrand: over `(a + c x^4)^3` with `(d + e x)^2` beside it the
    /// answer was wrong at every point, and is declined now.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1838">#1838</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALinearBesideABlockIsCheckedIntegralTest
    {
        [Theory]
        [InlineData("1/((d + g*x)^2*(a + c*x^4)^3)")]
        [InlineData("1/((d + g*x)*(a + c*x^4)^3)")]
        public void NoAnswerIsWrong(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            if (integral.Stringize().Contains("integral("))
                return;
            Entity Pinned(Entity e) => e.Substitute("a", 1).Substitute("c", 3).Substitute("d", 1).Substitute("g", 2);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.2, -0.4, 0.31, 0.83, 1.29, 2.5 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || got.IsNaN)
                    continue;
                compared++;
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} points could be compared for {integrand}");
        }
    }
}
