//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>b ^ e mod m</c> and <c>n! mod m</c> for whole numbers, reduced modulo <c>m</c> as they are
    /// built. The power or the factorial was written out first: <c>2 ^ (10 ^ 8) mod 1000</c> took
    /// 29 s and 13.9 GB, and <c>10 ^ (10 ^ 9) mod 7</c> did not finish.
    /// https://github.com/asc-community/AngouriMath/pull/2327#issuecomment-6575158568
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ARemainderOfALargeWholeTest
    {
        // Each expected value is Python's: pow(b, e, m), math.factorial(n) % m, or the product
        // reduced as it goes, and the operator's floored remainder takes the divisor's sign.
        [Theory]
        [InlineData("10 ^ (10 ^ 9) mod 7", 4)]
        [InlineData("2 ^ (10 ^ 8) mod 1000", 376)]
        [InlineData("(2 ^ 100) ^ (10 ^ 9) mod 1000000007", 25600497)]
        [InlineData("(-3) ^ 5 mod 7", 2)]
        [InlineData("(-3) ^ 5 mod (-7)", -5)]
        [InlineData("3 ^ 5 mod (-7)", -2)]
        [InlineData("7 ^ 0 mod 5", 1)]
        [InlineData("0 ^ 3 mod 5", 0)]
        [InlineData("5 ^ 3 mod 1", 0)]
        [InlineData("(10 ^ 5)! mod 1000003", 353746)]
        // Wilson: (p - 1)! = -1 mod p, so (p - 3)! = -1/2, which is (p - 1)/2.
        [InlineData("(10 ^ 6)! mod 1000003", 500001)]
        [InlineData("(10 ^ 7)! mod 1000000007", 682498929)]
        [InlineData("(10 ^ 6)! mod (10 ^ 12 + 39)", 981156865440)]
        [InlineData("20! mod 7", 0)]
        [InlineData("6! mod (-7)", -1)]
        [InlineData("0! mod 3", 1)]
        public void IsTheRemainder(string expression, long expected)
        {
            Assert.Equal(Integer.Create(expected), ((Entity)expression).Evaled);
            Assert.Equal(Integer.Create(expected), ((Entity)expression).InnerSimplified);
        }

        // 0 ^ 0 has no value, and a power that is not whole is the remainder it was.
        [Fact]
        public void ZeroToTheZeroHasNone() => Assert.True(((Entity)"0 ^ 0 mod 5").Evaled.IsNaN);

        [Fact]
        public void AReciprocalIsNotWhole() => Assert.Equal(Rational.Create(1, 2), ((Entity)"2 ^ (-1) mod 3").Evaled);

        // Writing the power out took 772 MB; reduced as it is built it takes under 1 MB. Bytes
        // allocated on this thread are what is measured, so tests running beside it do not count.
        [Fact]
        public void ThePowerIsNotWrittenOut()
        {
            Entity remainder = "2 ^ (10 ^ 7) mod 1000";
            var before = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(Integer.Create(376), remainder.Evaled);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(allocated < 20_000_000, $"2 ^ (10 ^ 7) mod 1000 allocated {allocated} bytes");
        }
    }
}
