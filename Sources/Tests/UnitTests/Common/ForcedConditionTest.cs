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
    /// A conjunct of a condition that another conjunct already forces says nothing more:
    /// <c>a ^ 2 provided a &gt; -5 and a &gt;= 0</c> is <c>a ^ 2 provided a &gt;= 0</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ForcedConditionTest
    {
        [Theory]
        [InlineData("x provided a > 1 and a > 2", "x provided a > 2")]
        [InlineData("x provided a > 2 and a > 1", "x provided a > 2")]
        [InlineData("x provided a >= 0 and a > -5", "x provided a >= 0")]
        [InlineData("x provided a >= 0 and a >= 0", "x provided a >= 0")]
        [InlineData("(x provided a > 0) provided a >= 0", "x provided a > 0")]
        [InlineData("(x provided a >= 0) provided a > 0", "x provided a > 0")]
        public void AConjunctAnotherForcesIsNotStated(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>Conjuncts that force nothing of each other are all kept, in the order written.</summary>
        [Theory]
        [InlineData("x provided a > 0 and b > 0")]
        [InlineData("x provided a > 0 and a < 5")]
        public void ConjunctsThatForceNothingAreKept(string written)
            => Assert.Equal(written.ToEntity(), written.ToEntity().InnerSimplified);

        /// <summary>
        /// Two operands carrying the same condition, or one forcing the other's, give a result that
        /// states it once. A second derivative of <c>1/sqrt(x^2 + 13)</c> stated
        /// <c>not x ^ 2 + 13 = 0</c> three times.
        /// </summary>
        [Theory]
        [InlineData("(a provided b > 0) + (c provided b > 0)", "a + c provided b > 0")]
        [InlineData("(a provided b > 0) * (c provided b > 1)", "a * c provided b > 1")]
        [InlineData("(a provided b > 0) + (c provided b > 0) + (d provided b > 0)", "a + c + d provided b > 0")]
        [InlineData("log(a provided b > 0, c provided b > 0)", "log(a, c) provided b > 0")]
        public void OperandsConditionsAreJoinedOnce(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        [Fact]
        public void ASecondDerivativeStatesItsConditionOnce()
        {
            var derivative = "derivative(1 / sqrt(x^2 + 13), x, 2)".ToEntity().InnerSimplified;
            var condition = Assert.IsType<Entity.Providedf>(derivative).Predicate;
            Assert.Equal("not x ^ 2 + 13 = 0".ToEntity(), condition);
        }

        [Fact]
        public void ASolvedRootStatesItOnce()
            => Assert.Equal("{ a ^ 2 provided a >= 0 }".ToEntity(), "(sqrt(x) = a) provided a > -5".ToEntity().Solve("x"));
    }
}
