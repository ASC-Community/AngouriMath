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
    /// A sum of logarithms or of arctangents times a number is the sum. The solvers for those read
    /// the number into each function's multiplier, so <c>(ln(-3x) + ln(1 - x)) 10^12 = 0</c> raised
    /// each argument to the 10^12th power and threw an <see cref="System.OverflowException"/>
    /// working out the degree, and a halved sum of arctangents was declined for its multipliers not
    /// being whole. Found by solving generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AMultipleOfASumOfFunctionsTest
    {
        [Theory]
        [InlineData("ln(-3x) + ln(1 - x) = 0", "10^12")]
        [InlineData("ln(-3x) + ln(1 - x) = 0", "10^(-12)")]
        [InlineData("ln(x) + ln(x + 1) = 1", "10^12")]
        [InlineData("arctan(x) + arctan(2x) = -1", "10^12")]
        [InlineData("arctan(x) + arctan(3x) = -2", "10^(-12)")]
        [InlineData("arctan(x) + arctan(2x) = 1", "1/2")]
        public void TheRootsAreThoseOfTheSum(string equation, string scale)
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

        // Exact, as the sum is: the arctangent solver takes the tangent of a halved sum as well.
        [Fact]
        public void AHalvedSumOfArctangentsIsSolvedExactly()
        {
            var answer = Assert.IsType<FiniteSet>("(arctan(x) + arctan(2x)) / 2 = 1/2".ToEntity().Solve("x"));
            Assert.NotEmpty(answer);
            Assert.DoesNotContain(answer, root => root is Number.Real and not Number.Rational);
        }
    }
}
