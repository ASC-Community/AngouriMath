//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A power of a constant such as <c>ln(2)</c> or <c>pi</c> cancels between the numerator
    /// and the denominator of a quotient, as a power of a variable does. The factors were
    /// paired only when both sides held a variable, so <c>ln(2) / (x ln(2)^2)</c>, the
    /// derivative of <c>log(2, x)</c>, was left as written.
    /// https://github.com/asc-community/AngouriMath/issues/2603
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AConstantFactorIsPairedAcrossAQuotientTest
    {
        [Theory]
        [InlineData("ln(2) / (x * ln(2)^2)", "1 / (x * ln(2))")]
        [InlineData("pi / (x * pi^2)", "1 / (x * pi)")]
        [InlineData("sin(1) / (x * sin(1)^2)", "1 / (x * sin(1))")]
        [InlineData("ln(2)^2 / (x * ln(2))", "ln(2) / x")]
        [InlineData("ln(2) * x / ln(2)^2", "x / ln(2)")]
        [InlineData("ln(3)^3 / (x * ln(3)^5)", "1 / (x * ln(3)^2)")]
        public void Cancels(string quotient, string expected)
        {
            var simplified = quotient.ToEntity().Simplify();
            // The cancelled form or one as small, since operand order is not the point here.
            var cancelled = expected.ToEntity().Simplify();
            Assert.True(simplified.Complexity <= cancelled.Complexity, $"{simplified} against {cancelled}");
            foreach (var x in new[] { -3.5, -1, 0.25, 2, 7 })
            {
                var before = (double)((Entity.Number.Real)quotient.ToEntity().Substitute("x", x).Evaled).EDecimal.ToDouble();
                var after = (double)((Entity.Number.Real)simplified.Substitute("x", x).Evaled).EDecimal.ToDouble();
                Assert.True(Math.Abs(before - after) <= 1e-12 * Math.Max(1, Math.Abs(before)), $"{before} against {after} at x = {x}");
            }
        }

        [Fact]
        public void TheDerivativeOfALogarithmToAConstantBase()
        {
            var derivative = "log(2, x)".ToEntity().Differentiate("x").Simplify();
            Assert.True(derivative.Complexity <= "1 / (x * ln(2))".ToEntity().Simplify().Complexity, derivative.ToString());
            Assert.Equal(1 / (3 * Math.Log(2)), (double)((Entity.Number.Real)derivative.Substitute("x", 3).Evaled).EDecimal.ToDouble(), 12);
        }
    }
}
