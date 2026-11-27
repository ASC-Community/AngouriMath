//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// <c>ln(-1)</c>, <c>ln(i)</c> and <c>ln(-i)</c> were never folded, so a trigonometric root was
    /// written <c>ln(-1) / i</c> where it is <c>pi</c>, and <c>Simplify</c> left it as
    /// <c>(-i) * ln(-1)</c>. Each is <c>i</c> times its argument in <c>(-pi; pi]</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2581
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class LogarithmOfAUnitPointIsFoldedTest
    {
        [Theory]
        [InlineData("ln(-1)", "i pi")]
        [InlineData("ln(i)", "i pi / 2")]
        [InlineData("ln(-i)", "-i pi / 2")]
        [InlineData("ln(-1) / i", "pi")]
        [InlineData("ln(i) / i", "pi / 2")]
        public void TheLogarithmIsITimesTheArgument(string expression, string expected)
        {
            var folded = expression.ToEntity().Simplify();
            Assert.DoesNotContain(folded.Nodes, node => node is Logf);
            Assert.Equal(Number.Integer.Zero, (folded - expected.ToEntity()).Simplify());
        }

        /// <summary>Over the reals <c>ln(-1)</c> has no value, and is not given <c>i pi</c> there.</summary>
        [Fact]
        public void OverTheRealsItIsNotFolded()
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            Assert.DoesNotContain("ln(-1)".ToEntity().Simplify().Nodes, node => node == MathS.i);
        }

        [Theory]
        [InlineData("sin(2x) = sin(x)", "pi + 2 * pi * n")]
        [InlineData("sin(x) + cos(x) = 1", "pi / 2 + 2 * pi * n")]
        public void ATrigonometricRootIsWrittenAsItsAngle(string equation, string root)
        {
            var solved = equation.ToEntity().Solve("x").Stringize();
            Assert.Contains(root, solved);
            Assert.DoesNotContain("ln(-1)", solved);
            Assert.DoesNotContain("ln(i)", solved);
        }
    }
}
