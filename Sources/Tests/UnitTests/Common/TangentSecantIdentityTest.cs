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
    /// <c>1 + tan(x)^2 = sec(x)^2</c> was found for two siblings only, and secant sorts before
    /// tangent, so <c>1 + tan(x)^2 - sec(x)^2</c> became <c>1 - sec(x)^2 + tan(x)^2</c> and stayed,
    /// while its cotangent twin, where cotangent sorts first, was <c>0</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2142">#2142</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TangentSecantIdentityTest
    {
        [Theory]
        [InlineData("1 + tan(x)^2 - sec(x)^2", "0")]
        [InlineData("1 + cot(x)^2 - cosec(x)^2", "0")]
        [InlineData("a + 1 + tan(y)^2 - sec(y)^2", "a")]
        [InlineData("tan(x)^2 - sec(x)^2 + 1", "0")]
        public void TheIdentityHolds(string input, string expected)
        {
            var difference = (input.Simplify() - expected.ToEntity()).Simplify();
            while (difference is Providedf(var inner, _))
                difference = inner;
            Assert.Equal(Integer.Zero, difference);
        }

        /// <summary>
        /// Where the tangent is undefined the answer is too, as the input is.
        /// </summary>
        [Fact]
        public void TheAnswerKeepsTheTangentsDomain()
            => Assert.Contains("cos(x) = 0", "1 + tan(x)^2 - sec(x)^2".Simplify().Stringize());
    }
}
