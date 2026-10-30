//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The factorial, and the gamma function with it, is defined everywhere but at the negative integers, its poles, where
    /// its condition asked for a real argument as well: <c>i!</c> is <c>0.498 - 0.155i</c>, and <c>x!/x!</c> simplified to
    /// <c>1 provided x in RR</c>, which has no value at <c>i</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2256">#2256</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class FactorialOffTheRealLineTest
    {
        [Theory]
        [InlineData("x!", "i", true)]
        [InlineData("x!", "1/2 + 6/5 * i", true)]
        [InlineData("x!", "-2 * i", true)]
        [InlineData("x!", "-1/2", true)]
        [InlineData("x!", "0", true)]
        [InlineData("x!", "3", true)]
        [InlineData("x!", "-1", false)]
        [InlineData("x!", "-2", false)]
        [InlineData("gamma(x)", "i", true)]
        [InlineData("gamma(x)", "0", false)]
        [InlineData("gamma(x)", "-3", false)]
        public void ItIsDefinedOffThePoles(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainCondition.Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
            Assert.Equal(!defined, expression.ToEntity().Substitute("x", at.ToEntity()).Evaled.IsNaN);
        }

        [Theory]
        [InlineData("x! / x!", "i")]
        [InlineData("gamma(x) / gamma(x)", "1/2 + 6/5 * i")]
        public void ASimplificationKeepsItsValueThere(string expression, string at)
            => Assert.Equal(Entity.Number.Integer.One, expression.ToEntity().Simplify().Substitute("x", at.ToEntity()).Evaled);
    }
}
