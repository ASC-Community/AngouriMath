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
    /// Two tangents, cotangents, secants or cosecants of arguments whose difference or sum is a
    /// constant, written as the functions of each apart by the addition formulas. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 0.3</c>, <c>b = 0.9</c>, <c>c = 1.1</c>, on
    /// both sides of zero and away from the poles of every function of the three arguments.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class TwoFunctionsOfShiftedArgumentsIntegralTest
    {
        [Theory]
        [InlineData("tan(a + b*x)*tan(c + b*x)")]
        [InlineData("tan(c - b*x)*tan(a + b*x)")]
        [InlineData("cot(a + b*x)*cot(c + b*x)")]
        [InlineData("cot(c - b*x)*cot(a + b*x)")]
        [InlineData("sec(a + b*x)*sec(c + b*x)")]
        [InlineData("sec(c - b*x)*sec(a + b*x)")]
        [InlineData("csc(a + b*x)*csc(c + b*x)")]
        [InlineData("csc(c - b*x)*csc(a + b*x)")]
        public void IsWrittenApartByTheAdditionFormulas(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.3).Substitute("b", 0.9).Substitute("c", 1.1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.0, -0.8, 0.1, 0.2, 0.9, 1.0 })
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
