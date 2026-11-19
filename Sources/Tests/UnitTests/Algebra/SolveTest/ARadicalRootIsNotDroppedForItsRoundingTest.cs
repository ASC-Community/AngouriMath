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
    /// An equation in a modulus is solved as a system for the parts of <c>x</c>, and a root of the
    /// system's last equation that leaves another a nonzero number is dropped. A root with a radical
    /// in it is put in by evaluating, and where the terms are large the evaluation leaves a rational
    /// that is not 0 although the root is one: so the real roots of these equations, near
    /// <c>10^23</c> or more, were dropped and the answer was <c>{ }</c>. Found by solving generated
    /// equations with <c>x</c> written as <c>10^-30 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARadicalRootIsNotDroppedForItsRoundingTest
    {
        [Theory]
        [InlineData("(1 - 2 * {0}) + {0} * abs(-2 - 3 * {0})", 1)]
        [InlineData("-2 * {0} + 3 * {0}^2 - 2 * abs(-3 + 2 * {0})", 2)]
        public void EveryRootIsFoundAtScale(string shape, int count)
        {
            foreach (var digits in new[] { 0, 23, 24 })
            {
                var scaled = string.Format(shape, $"(10^(-{digits}) * x)");
                var answer = Assert.IsType<FiniteSet>($"{scaled} = 0".ToEntity().Solve("x"));
                Assert.Equal(count, answer.Count);
                var unscaled = string.Format(shape, "x").ToEntity();
                foreach (var root in answer)
                {
                    var residual = Assert.IsAssignableFrom<Number.Complex>(unscaled.Substitute("x", root * MathS.Pow(10, -digits)).EvalNumerical());
                    Assert.True(residual.Abs() < 1e-30, $"{root} leaves {residual} at 10^{digits}");
                }
            }
        }
    }
}
