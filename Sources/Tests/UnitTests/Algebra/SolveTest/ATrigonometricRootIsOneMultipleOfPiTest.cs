//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A root of <c>sin(x) = value</c> or <c>cos(x) = value</c> at a rational multiple of pi was written
    /// as the sum it was built from: <c>cos(x) = -1/2</c> had <c>pi / 2 - -pi / 6</c>, and
    /// <c>sin(x) = 1</c> listed <c>pi / 2</c> and <c>pi - pi / 2</c>, one root twice. Each is now one
    /// multiple, taken in <c>(-pi; pi]</c> since a family <c>2 pi</c> apart follows it.
    /// https://github.com/asc-community/AngouriMath/issues/2581
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ATrigonometricRootIsOneMultipleOfPiTest
    {
        [Theory]
        [InlineData("cos(x) = -1/2", "union({ 2/3 * pi + 2 * pi * n, -2/3 * pi + 2 * pi * n }, n in ZZ)")]
        [InlineData("sin(x) = 1", "union({ pi / 2 + 2 * pi * n }, n in ZZ)")]
        [InlineData("sin(x) = -1", "union({ -pi / 2 + 2 * pi * n }, n in ZZ)")]
        [InlineData("cos(x) = -1", "union({ pi + 2 * pi * n }, n in ZZ)")]
        [InlineData("sin(x) = -1/2", "union({ -pi / 6 + 2 * pi * n, -5/6 * pi + 2 * pi * n }, n in ZZ)")]
        [InlineData("2cos(x)^2 + cos(x) - 1 = 0", "union({ pi + 2 * pi * n, pi / 3 + 2 * pi * n, -pi / 3 + 2 * pi * n }, n in ZZ)")]
        public void TheRootIsWrittenOnce(string equation, string expected)
            => Assert.Equal(expected, equation.ToEntity().Solve("x").Stringize());

        /// <summary>A value whose angle is not a known multiple of pi is left as the inverse function.</summary>
        [Theory]
        [InlineData("sin(x) = a", "union({ arcsin(a) + 2 * pi * n, pi - arcsin(a) + 2 * pi * n }, n in ZZ)")]
        [InlineData("cos(x) = 1/3", "union({ arccos(1/3) + 2 * pi * n, -arccos(1/3) + 2 * pi * n }, n in ZZ)")]
        public void AnyOtherAngleIsAsBefore(string equation, string expected)
            => Assert.Equal(expected, equation.ToEntity().Solve("x").Stringize());
    }
}
