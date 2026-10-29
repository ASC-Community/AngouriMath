//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>forwarddifference(f, k)</c> is <c>f(k + 1) - f(k)</c>, as Mathematica's
    /// <c>DifferenceDelta[f, k]</c>. The name was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ForwardDifferenceTest
    {
        [Theory]
        [InlineData("forwarddifference(k^2, k)", "2 k + 1")]
        [InlineData("forwarddifference(2^k, k)", "2^k")]
        [InlineData("forwarddifference(a k + b, k)", "a")]
        [InlineData("forwarddifference(x, k)", "0")]
        public void ItIsTheValueAtTheNextIntegerLessTheValue(string written, string expected)
            => Assert.Equal(Number.Integer.Zero, (written.ToEntity() - expected.ToEntity()).Simplify());

        /// <summary>
        /// The difference of a factorial is k k!, checked by its value: Simplify brings
        /// (k + 1)! - (k + 1) k! to 0, but not (k + 1)! - k! - k k!.
        /// </summary>
        [Fact]
        public void TheDifferenceOfAFactorialIsKTimesIt()
            => Assert.Equal(Number.Integer.Create(600), "forwarddifference(k!, k)".ToEntity().Substitute("k", 5).Evaled);

        /// <summary>A sum of forward differences telescopes: the sum over 1..5 of (k + 1)^3 - k^3 is 6^3 - 1.</summary>
        [Fact]
        public void ASumOfDifferencesTelescopes()
            => Assert.Equal(Number.Integer.Create(215), "sum(forwarddifference(k^3, k), k, 1, 5)".ToEntity().Evaled);

        /// <summary>The derivative of a difference is the difference of the derivative, which needs the derivative at k + 1.</summary>
        [Fact]
        public void ItReachesADerivative()
            => Assert.Equal(Number.Integer.Zero,
                ("forwarddifference(derivative(k^3, k), k)".ToEntity() - "6 k + 3".ToEntity()).Simplify());

        [Theory]
        [InlineData("forwarddifference(k^2, 2)")]
        [InlineData("forwarddifference(k^2, pi)")]
        public void ItIsTakenInAVariable(string written)
            => Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity());
    }
}
