//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>cos(x)^2 = (1 + cos(2x))/2</c> was not proved while its sine twin was: the difference
    /// reached <c>-1/2 + cos(x)^2 / 2 + sin(x)^2 / 2</c>, and a squared sine and cosine over one
    /// divisor was found by no rule.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2142">#2142</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PowerReductionTest
    {
        [Theory]
        [InlineData("cos(x)^2 - (1 + cos(2*x))/2", "0")]
        [InlineData("sin(x)^2 - (1 - cos(2*x))/2", "0")]
        [InlineData("sin(x)^2/3 + cos(x)^2/3", "1/3")]
        [InlineData("a + sin(t)^2/b + cos(t)^2/b", "a + 1/b")]
        public void TheIdentityHolds(string input, string expected)
        {
            // a + 1/b less itself is 0 provided b is not 0, and the condition is not what is compared.
            var difference = (input.Simplify() - expected.ToEntity()).Simplify();
            while (difference is AngouriMath.Entity.Providedf(var inner, _))
                difference = inner;
            Assert.Equal(Integer.Zero, difference);
        }

        /// <summary>Two different divisors are not the identity.</summary>
        [Fact]
        public void DifferentDivisorsAreLeft()
            => Assert.Contains("sin", "sin(x)^2/2 + cos(x)^2/3".Simplify().Stringize());
    }
}
