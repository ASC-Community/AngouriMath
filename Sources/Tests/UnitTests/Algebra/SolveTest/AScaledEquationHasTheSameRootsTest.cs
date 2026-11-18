//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// <c>L = R</c> and <c>(L - R) s = 0</c> have the same roots for any number <c>s</c> that is not
    /// 0. A candidate that cubing an equation adds was thrown out by its residual, but a residual
    /// below an absolute 1e-6 was taken for none, so at <c>s = 10^-12</c> every such candidate
    /// stayed: <c>x (2 - 4x + 2x^2)^(1/3) = 2</c> has three roots, and scaled it had five, two of
    /// which leave 3.46 times <c>s</c>. And the scaled equation is one product, which was read as
    /// one term and so as its own scale: <c>(sin(x) - x) 10^12 = 0</c> had only the root 0. Found
    /// by solving generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AScaledEquationHasTheSameRootsTest
    {
        [Theory]
        [InlineData("x * (2 - 4x + 2x^2)^(1/3) = 2", "10^(-12)")]
        [InlineData("x * (3 + 3x + 2x^2)^(1/3) = -2", "10^(-12)")]
        [InlineData("2 - 3x^2 - (-3x)^(1/3) = 0", "10^(-12)")]
        [InlineData("x * (2 - 4x + 2x^2)^(1/3) = 2", "10^12")]
        // Newton's roots, judged against the terms of the product, not against the product.
        [InlineData("sin(x) = x", "10^12")]
        [InlineData("sin(x) = x", "10^(-12)")]
        [InlineData("arctan(x) = x / 3", "10^(-12)")]
        [InlineData("x^2 - 2 = 0", "10^(-12)")]
        [InlineData("x^3 - 3x + 1 = 0", "10^(-30)")]
        public void TheRootsAreTheRootsOfTheEquationAsWritten(string equation, string scale)
        {
            var asWritten = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            var sides = equation.Split(" = ");
            var scaled = Assert.IsType<FiniteSet>(
                $"(({sides[0]}) - ({sides[1]})) * {scale} = 0".ToEntity().Solve("x"));
            Assert.Equal(asWritten.Count, scaled.Count);
            foreach (var root in scaled)
                Assert.Contains(asWritten, other =>
                    ((Number.Complex)(root - other).EvalNumerical()).Abs() < 1e-8);
        }
    }
}
