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
    /// A sum of logarithms or of arctangents, each written times the same constant that is not
    /// rational, has the roots of the sum. The solvers for those read only rational multipliers, so
    /// <c>sqrt(2) ln(x) + sqrt(2) ln(x + 1) = 0</c> went to Newton's method and came back as a
    /// decimal where <c>ln(x) + ln(x + 1) = 0</c> is <c>{ (sqrt(5) - 1) / 2 }</c>. Found by solving
    /// generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnIrrationalMultipleOfASumOfFunctionsTest
    {
        [Theory]
        [InlineData("sqrt(2) * ln(x) + sqrt(2) * ln(x + 1) = 0", "(sqrt(5) - 1) / 2")]
        [InlineData("pi * ln(x) + pi * ln(x + 1) = 0", "(sqrt(5) - 1) / 2")]
        [InlineData("sqrt(2) * ln(x) + sqrt(2) * ln(x + 1) = sqrt(2)", "(sqrt(1 + 4e) - 1) / 2")]
        [InlineData("e^(-50) * arctan(x) + e^(-50) * arctan(2x) = e^(-50) * 3 * pi / 4", "(3 + sqrt(17)) / 4")]
        public void TheRootIsExact(string equation, string expected)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            var root = Assert.Single(answer);
            Assert.False(root is Number, $"{equation} gave the decimal {root}");
            var difference = Assert.IsAssignableFrom<Number.Complex>((root - expected.ToEntity()).Evaled);
            Assert.True(difference.IsZero, $"{equation} gave {root}");
        }

        // Multipliers whose ratio is not rational are not a multiple of one sum, and are left as
        // they were.
        [Fact]
        public void UnlikeMultipliersAreLeftToTheNumericalSolver()
        {
            var answer = Assert.IsType<FiniteSet>("sqrt(3) * ln(x) - sqrt(2) * ln(x + 1) = 0".ToEntity().Solve("x"));
            var root = Assert.Single(answer);
            var residual = (Number.Complex)"sqrt(3) * ln(x) - sqrt(2) * ln(x + 1)".ToEntity().Substitute("x", root).EvalNumerical();
            Assert.True(residual.Abs() < 1e-10);
        }
    }
}
