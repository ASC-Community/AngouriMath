//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The signum of a value that does not exist has none. <c>sgn(NaN)</c> was 1, EDecimal's Sign of NaN, so
    /// <c>sgn(-1 provided false)</c> was 1 and <c>1 + sgn(-1 provided pi &lt; 3)</c> 2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2241">#2241</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class SignumOfNaNTest
    {
        [Theory]
        [InlineData("sgn(NaN)")]
        [InlineData("sgn(0/0)")]
        [InlineData("sgn(-1 provided false)")]
        [InlineData("1 + sgn(-1 provided pi < 3)")]
        [InlineData("sgn(i * NaN)")]
        public void HasNoValue(string expression)
        {
            Assert.True(expression.ToEntity().Evaled.IsNaN, $"{expression}: {expression.ToEntity().Evaled}");
            Assert.True(expression.ToEntity().Simplify().IsNaN, $"{expression}: {expression.ToEntity().Simplify()}");
        }

        [Theory]
        [InlineData("sgn(-3)", "-1")]
        [InlineData("sgn(0)", "0")]
        [InlineData("sgn(+oo)", "1")]
        [InlineData("sgn(-oo)", "-1")]
        [InlineData("sgn(-1 provided pi > 3)", "-1")]
        public void ElsewhereItIsTheSign(string expression, string expected)
            => Assert.Equal(expected.ToEntity(), expression.ToEntity().Evaled);

        /// <summary>Compiled, it has no value there too, as it already had.</summary>
        [Fact]
        public void CompiledItHasNoValue()
            => Assert.True(double.IsNaN("x + sgn(-1 provided pi < 3)".ToEntity().Compile("x").Call(1).Real));
    }
}
