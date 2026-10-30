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
    /// Over the reals the Lambert W function is defined where its branch is real: <c>W_0</c> for <c>z &gt;= -1/e</c> and
    /// <c>W_(-1)</c> for <c>-1/e &lt;= z &lt; 0</c>. The condition was the complex one under either reading, so it held for
    /// <c>W_0(-1)</c>, which is <c>-0.318 + 1.337i</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2256">#2256</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class LambertWOverTheRealsTest
    {
        [Theory]
        [InlineData("lambertw(0, x)", "-3", false)]
        [InlineData("lambertw(0, x)", "-1", false)]
        [InlineData("lambertw(0, x)", "-1/e", true)]
        [InlineData("lambertw(0, x)", "-1/5", true)]
        [InlineData("lambertw(0, x)", "0", true)]
        [InlineData("lambertw(0, x)", "2", true)]
        [InlineData("lambertw(-1, x)", "-1/e", true)]
        [InlineData("lambertw(-1, x)", "-1/5", true)]
        [InlineData("lambertw(-1, x)", "-1/2", false)]
        [InlineData("lambertw(-1, x)", "0", false)]
        [InlineData("lambertw(-1, x)", "1", false)]
        [InlineData("lambertw(1, x)", "1", false)]
        public void OverTheRealsItIsWhereItsBranchIsReal(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainConditionIn(Domain.Real).Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
        }

        /// <summary>Over the complex numbers every branch is defined but off the principal one at 0.</summary>
        [Theory]
        [InlineData("lambertw(0, x)", "-3", true)]
        [InlineData("lambertw(-1, x)", "1", true)]
        [InlineData("lambertw(-1, x)", "0", false)]
        public void OverTheComplexNumbersItIsAsItWas(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainCondition.Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
        }
    }
}
