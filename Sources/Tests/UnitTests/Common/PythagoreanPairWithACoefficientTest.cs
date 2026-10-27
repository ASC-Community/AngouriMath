//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>k sin(x)^2 + k cos(x)^2</c> was reduced only where the two terms were siblings, so the
    /// sort decided: <c>y + (-2) cos(x)^2 + (-2) sin(x)^2</c> came out <c>y - 2</c>, and
    /// <c>2 + (-2) cos(x)^2 + (-2) sin(x)^2</c>, where the constant sorts between the two, stayed.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2133">#2133</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PythagoreanPairWithACoefficientTest
    {
        [Theory]
        [InlineData("2 + (-2)*cos(x)^2 + (-2)*sin(x)^2", "0")]
        [InlineData("2 + (-2) * cos(2 * x) + (-4) * sin(x) ^ 2", "0")]
        [InlineData("a + b*sin(t)^2 + c + b*cos(t)^2", "a + b + c")]
        [InlineData("y + (-2)*cos(x)^2 + (-2)*sin(x)^2", "y - 2")]
        public void ThePairIsItsCoefficient(string input, string expected)
            => Assert.Equal(Integer.Zero, (input.Simplify() - expected.ToEntity()).Simplify());

        /// <summary>
        /// Two coefficients, or two arguments, that differ are not the identity.
        /// </summary>
        [Theory]
        [InlineData("2*sin(x)^2 + 3*cos(x)^2")]
        [InlineData("k*sin(x)^2 + k*cos(y)^2")]
        public void UnequalPartsAreLeft(string input)
            => Assert.Contains("sin", input.Simplify().Stringize());

        /// <summary>
        /// The first rows of #2133's table, which are rational identities, and are answered by the
        /// rational canonical form since
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2137">#2137</a>.
        /// </summary>
        [Theory]
        [InlineData("a*b*c*(1/a + 1/b + 1/c) - (a*b + b*c + c*a)")]
        [InlineData("(a*b + b*c + c*a)/3 - (a*b*c)*(1/a + 1/b + 1/c)/3")]
        public void TheRationalRowsAreZero(string input)
        {
            var simplified = input.Simplify();
            while (simplified is Providedf(var inner, _))
                simplified = inner;
            Assert.Equal(Integer.Zero, simplified);
        }
    }
}
