//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.PatternsTest
{
    /// <summary>
    /// A sine or cosine whose only radical form goes through the complex numbers, casus irreducibilis, stays as the
    /// function of its angle. <c>sin(pi/7)</c> simplified to <c>sqrt((1 - 1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) +
    /// ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) / 2)</c>, many times longer than the input, and no real radical form exists.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2143">#2143</a>
    /// </summary>
    [Trait("Area", "PatternsTest")]
    public sealed class CasusIrreducibilisTest
    {
        [Theory]
        [InlineData("sin(pi/7)")]
        [InlineData("cos(pi/7)")]
        [InlineData("cos(2 pi/7)")]
        [InlineData("sin(4 pi/7)")]
        [InlineData("sin(pi/9)")]
        [InlineData("cos(2 pi/9)")]
        [InlineData("sin(pi/18)")]
        public void ItStaysAsWritten(string value)
        {
            var simplified = value.ToEntity().Simplify();
            Assert.DoesNotContain(simplified.Nodes, node => node is Complex and not Real || node is Entity.Powf(_, Rational { ERational.Denominator.IsEven: false }));
            Assert.True(simplified.Nodes.Count() <= value.ToEntity().Nodes.Count() + 2, simplified.Stringize());
            var difference = (simplified - value.ToEntity()).EvalNumerical();
            Assert.True(difference.Abs() < 1e-30, $"{simplified} differs from {value} by {difference}");
        }

        /// <summary>A value with a real radical form still takes it.</summary>
        [Theory]
        [InlineData("sin(pi/5)", "1/4 * sqrt(10 - 2 * sqrt(5))")]
        [InlineData("sin(pi/3)", "sqrt(3) / 2")]
        [InlineData("cos(2 pi/5)", "(sqrt(5) - 1) / 4")]
        public void ARealRadicalIsStillTaken(string value, string expected)
            => Assert.Equal(expected, value.ToEntity().Simplify().Stringize());

        [Fact]
        public void TheIdentitiesStillHold()
            => Assert.Equal(Integer.One, "sin(pi/7)^2 + cos(pi/7)^2".ToEntity().Simplify());
    }
}
