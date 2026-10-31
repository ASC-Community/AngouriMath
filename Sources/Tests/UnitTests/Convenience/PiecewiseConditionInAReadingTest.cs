//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// A piecewise is defined under a reading where the case it takes is, as <see cref="Entity.DomainCondition"/> already
    /// reads it, where <see cref="Entity.DomainConditionIn(Domain)"/> asked every case to be defined:
    /// <c>tan(piecewise(pi / x provided x &gt; 0, x / 2))</c> is 0 at 0, and its condition over the reals failed there.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2256">#2256</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class PiecewiseConditionInAReadingTest
    {
        [Theory]
        [InlineData("tan(piecewise(pi / x provided x > 0, x / 2))", "0", true)]
        [InlineData("tan(piecewise(pi / x provided x > 0, x / 2))", "-2", true)]
        [InlineData("tan(piecewise(pi / x provided x > 0, x / 2))", "2", false)]
        [InlineData("piecewise(arccos(1 / x) provided x > 0, sec(sgn(x)))", "-1/2", true)]
        [InlineData("piecewise(arccos(1 / x) provided x > 0, sec(sgn(x)))", "1/2", false)]
        [InlineData("piecewise(sqrt(x) provided x > 0, 1)", "-4", true)]
        [InlineData("piecewise(sqrt(x - 1) provided x > 0, 1)", "1/2", false)]
        public void OverTheRealsItIsWhereTheCaseItTakesIs(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainConditionIn(Domain.Real).Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
        }

        /// <summary>Over the complex numbers it is what <see cref="Entity.DomainCondition"/> says.</summary>
        [Theory]
        [InlineData("tan(piecewise(pi / x provided x > 0, x / 2))", "0")]
        [InlineData("tan(piecewise(pi / x provided x > 0, x / 2))", "2")]
        [InlineData("piecewise(sqrt(x - 1) provided x > 0, 1)", "1/2")]
        public void OverTheComplexNumbersItIsTheDomainCondition(string expression, string at)
            => Assert.Equal(
                expression.ToEntity().DomainCondition.Substitute("x", at.ToEntity()).Evaled,
                expression.ToEntity().DomainConditionIn(Domain.Complex).Substitute("x", at.ToEntity()).Evaled);
    }
}
