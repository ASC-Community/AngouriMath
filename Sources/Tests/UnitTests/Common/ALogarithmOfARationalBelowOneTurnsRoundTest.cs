//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The logarithm of a rational between 0 and 1 is offered as minus the logarithm of its
    /// reciprocal, and the ranking decides. <c>ln(1/b) = -ln(b)</c> matched only a written
    /// quotient, never the literal <c>1/2</c>, so <c>-ln(1/2)</c> was left as written.
    /// https://github.com/asc-community/AngouriMath/issues/2617
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ALogarithmOfARationalBelowOneTurnsRoundTest
    {
        [Theory]
        [InlineData("-ln(1/2)", "ln(2)")]
        [InlineData("-ln(2/3)", "ln(3/2)")]
        [InlineData("-x * ln(1/2)", "x * ln(2)")]
        [InlineData("-log(10, 1/100)", "2")]
        [InlineData("ln(1/2)", "ln(1/2)")]
        [InlineData("ln(2/3)", "ln(2/3)")]
        [InlineData("ln(1/2) + ln(2)", "0")]
        [InlineData("-ln(2)", "-ln(2)")]
        [InlineData("-3 * ln(1/2)", "3 * ln(2)")]
        [InlineData("e^(x * ln(1/2))", "(1/2)^x")]
        public void IsTurnedRoundWhereThatIsSimpler(string expression, string expected)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.Equal(expected.ToEntity().InnerSimplified, simplified.InnerSimplified);
            foreach (var x in new[] { -2.5, 0.5, 3 })
                Assert.True(((Entity.Number.Complex)(expression.ToEntity().Substitute("x", x).Evaled
                    - simplified.Substitute("x", x).Evaled).Evaled).IsZero
                    || ((Entity.Number.Complex)(expression.ToEntity().Substitute("x", x).Evaled
                    - simplified.Substitute("x", x).Evaled).Evaled).Abs() < 1e-30,
                    $"{expression} and {simplified} at x = {x}");
        }
    }
}
