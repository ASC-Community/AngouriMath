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
    /// A difference offered to the substitution search as a sum is: <c>sin(1 + 2/(1 - x))</c> is
    /// <c>-sin(1 + 2/u)</c> under <c>u = 1 - x</c>, as <c>sin(1 + 2/(1 + x))</c> is
    /// <c>sin(1 + 2/u)</c> under <c>u = 1 + x</c>, and only the second was answered.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>p = 0.3</c>, <c>k = 0.9</c>, <c>c = 1.1</c>,
    /// <c>d = 0.7</c>, on both sides of the pole of every row.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DifferenceAsASubstitutionIntegralTest
    {
        [Theory]
        [InlineData("sin(1 + 2/(1 - x))")]
        [InlineData("cos(1 + 2/(1 - x))")]
        [InlineData("sin(1 + 2/(x - 1))")]
        [InlineData("sin(1 + 2/(3 - 2*x))")]
        [InlineData("sin(p + k/(c - d*x))")]
        public void IsSubstitutedForAsASumIs(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("p", 0.3).Substitute("k", 0.9).Substitute("c", 1.1).Substitute("d", 0.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.0, 0.3, 0.8, 2.2, 3.0 })
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
