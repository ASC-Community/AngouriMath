//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Compiled over <see cref="double"/>, a power of a value that does not exist, or to such a power, has none, as in
    /// evaluation and FastExpression, where <see cref="System.Math.Pow(double, double)"/> made <c>NaN^0</c> and <c>1^NaN</c> 1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2250">#2250</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PowerOfNaNOverDoubleTest
    {
        [Theory]
        [InlineData("(x provided x > 1)^0", 0.7)]
        [InlineData("1^(x provided x > 1)", 0.7)]
        [InlineData("((x provided x > 1)^Im(x)) * sin(-2)", 0.7)]
        [InlineData("x^0", double.NaN)]
        [InlineData("1^x", double.NaN)]
        public void ItHasNoValue(string expression, double at)
        {
            var overDoubles = expression.ToEntity().Compile<double, double>("x")(at);
            Assert.True(double.IsNaN(overDoubles), $"{expression} at {at}: {overDoubles} over double");
            Assert.True(double.IsNaN(expression.ToEntity().Compile("x").Call(at).Real), $"{expression} at {at} by FastExpression");
            if (!double.IsNaN(at))
                Assert.True(expression.ToEntity().Substitute("x", at).Evaled.IsNaN, $"{expression} at {at} evaluates to a value");
        }

        [Fact]
        public void OverFloatToo()
        {
            Assert.True(float.IsNaN("x^0".ToEntity().Compile<float, float>("x")(float.NaN)));
            Assert.True(float.IsNaN("1^x".ToEntity().Compile<float, float>("x")(float.NaN)));
        }

        [Theory]
        [InlineData("x^0", 0.7, 1)]
        [InlineData("1^x", 0.7, 1)]
        [InlineData("x^2", -3, 9)]
        [InlineData("x^(1/2)", 4, 2)]
        public void WhereItHasOneItIsThatValue(string expression, double at, double value)
            => Assert.Equal(value, expression.ToEntity().Compile<double, double>("x")(at));
    }
}
