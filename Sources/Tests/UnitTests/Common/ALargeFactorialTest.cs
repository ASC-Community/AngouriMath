//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Numerics;
using AngouriMath;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// An exact factorial. Every factorial up to the largest one asked for was kept, each made from
    /// the one before by a single multiplication, so <c>(10^5)!</c> took 21 s and left 8.6 GB
    /// behind after a full collection. Past the small ones it is now a balanced product and is not
    /// kept. https://github.com/asc-community/AngouriMath/pull/2327#issuecomment-6575158568
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ALargeFactorialTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(17)]
        [InlineData(999)]
        [InlineData(1000)]
        [InlineData(1001)]
        [InlineData(1017)]
        [InlineData(2500)]
        public void IsTheProductOfTheWholeNumbersUpToIt(int n)
        {
            var product = BigInteger.One;
            for (var i = 2; i <= n; i++)
                product *= i;
            Assert.Equal(Integer.Create(PeterO.Numbers.EInteger.FromString(product.ToString())), ((Entity)$"{n}!").Evaled);
        }

        // 1000003 is prime, and the remainder is Python's math.factorial(10**5) % 1000003.
        [Fact]
        public void TheRemainderOfAHundredThousandFactorial()
            => Assert.Equal(Integer.Create(353746), ((Entity)"(10^5)! mod 1000003").Evaled);

        // The running product copied the whole of itself once per factor: 276 MB for 20000!, which
        // is 33 KB. The balanced product allocates about 1 MB. Bytes allocated on this thread are
        // what is measured, so other tests running beside it do not move the figure.
        [Fact]
        public void IsNotMadeOneFactorAtATime()
        {
            Entity factorial = "20000!";
            var before = GC.GetAllocatedBytesForCurrentThread();
            _ = factorial.Evaled;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(allocated < 20_000_000, $"20000! allocated {allocated} bytes");
        }
    }
}
