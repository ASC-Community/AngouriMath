//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A rational root is searched for over the divisors of the first and last coefficients, and
    /// was only where both were below a million. The cubic <c>9x^3 + 9x^2 + 5x + 1</c>, with its
    /// roots scaled by <c>10^-12</c>, has coefficients past that and as cheap to factor, and its
    /// rational root <c>-1/(3 10^12)</c> was written by Cardano's formula. A system that needed it
    /// exactly, for the parts of x in <c>1 + 2x + 3x^2 - 2 Im(x) = 0</c> with x written as
    /// <c>10^12 x</c>, was declined. Found by solving generated equations with <c>x</c> written as
    /// <c>10^12 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARationalRootIsFoundAtAnyScaleTest
    {
        [Theory]
        [InlineData("x^3 + 1/10^12*x^2 + 5/(9*10^24)*x + 1/(9*10^36) = 0", "-1/3000000000000")]
        [InlineData("9*10^36*x^3 + 9*10^24*x^2 + 5*10^12*x + 1 = 0", "-1/3000000000000")]
        [InlineData("x^3 + 1/10^6*x^2 + 5/(9*10^12)*x + 1/(9*10^18) = 0", "-1/3000000")]
        [InlineData("x^3 + 1/10^30*x^2 + 5/(9*10^60)*x + 1/(9*10^90) = 0", "-1/3000000000000000000000000000000")]
        public void TheRationalRootIsExact(string equation, string root)
            => Assert.Contains(root.ToEntity().Evaled, Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x")));

        [Theory]
        [InlineData("10^9")]
        [InlineData("10^12")]
        public void AnEquationInAPartOfXIsSolvedAtScale(string scale)
        {
            var x = $"({scale} * x)";
            var answer = Assert.IsType<FiniteSet>($"1 + 2 * {x} + 3 * {x}^2 - 2 * Im({x}) = 0".ToEntity().Solve("x"));
            Assert.Equal(2, answer.Count);
            var unscaled = "1 + 2 * x + 3 * x^2 - 2 * Im(x)".ToEntity();
            foreach (var root in answer)
            {
                var residual = Assert.IsAssignableFrom<Number.Complex>(unscaled.Substitute("x", root * scale.ToEntity()).EvalNumerical());
                Assert.True(residual.Abs() < 1e-30, $"{root} leaves {residual}");
            }
        }
    }
}
