//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A decimal is made into a number under a tolerance that takes a value below half the working
    /// digits for what a cancellation left of 0, and a literal was made the same way, so
    /// <c>1.0E-51</c> parsed as <c>0</c> and <c>x = 1.0E-60</c> was solved as <c>{ 0 }</c>. Found
    /// while solving generated equations with <c>x</c> written as <c>10^12 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ASmallDecimalLiteralIsTheNumberWrittenTest
    {
        [Theory]
        [InlineData("1.0E-51", "51")]
        [InlineData("1.5E-60", "60")]
        [InlineData("1.0E-300", "300")]
        [InlineData("2.5E-40", "40")]
        public void TheLiteralKeepsItsValue(string literal, string digits)
        {
            var number = Assert.IsAssignableFrom<Number.Real>(literal.ToEntity());
            Assert.False(number.IsZero);
            // Scaled back up exactly, the literal is its mantissa.
            var scaled = Assert.IsAssignableFrom<Number.Real>((literal.ToEntity() * MathS.Pow(10, int.Parse(digits))).Evaled);
            Assert.Equal(literal.Split('E')[0].ToEntity().Evaled, scaled);
        }

        [Fact]
        public void AnEquationWithASmallLiteralIsSolvedToIt()
        {
            var answer = Assert.IsType<Set.FiniteSet>("x = 1.0E-60".ToEntity().Solve("x"));
            var root = Assert.IsAssignableFrom<Number.Real>(Assert.Single(answer));
            Assert.False(root.IsZero);
        }

        /// <summary>A literal near a small rational is still that rational.</summary>
        [Theory]
        [InlineData("0.5", "1/2")]
        [InlineData("0.333333333333333333333333333333333333333333333333333333333", "1/3")]
        public void ALiteralNearASmallRationalIsThatRational(string literal, string rational)
            => Assert.Equal(rational.ToEntity().Evaled, literal.ToEntity());
    }
}
