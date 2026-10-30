//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core.Sets
{
    /// <summary>
    /// Primality by Miller–Rabin to the first thirteen primes, exact below <c>3317044064679887385961981</c> (Sorenson and
    /// Webster, Math. Comp. 86 (2017)), and by Lucas–Lehmer for a Mersenne number; past both a witness still proves a
    /// number composite and a number that passes is left open. It was trial division, which for a prime just below
    /// <c>2^63</c> never finished, the square of the divisor overflowing.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2230">#2230</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class PrimalityTest
    {
        [Theory]
        // The largest prime below 2^63, where trial division did not finish.
        [InlineData("9223372036854775783 in PP", "True")]
        [InlineData("9223372036854775781 in PP", "False")]
        [InlineData("(2^61 - 1) in PP", "True")]
        // Mersenne numbers, by Lucas–Lehmer: 2^89 - 1 and 2^127 - 1 are prime, 2^67 - 1 and 2^101 - 1 are not.
        [InlineData("(2^89 - 1) in PP", "True")]
        [InlineData("(2^127 - 1) in PP", "True")]
        [InlineData("(2^67 - 1) in PP", "False")]
        [InlineData("(2^101 - 1) in PP", "False")]
        // 2^4423 - 1 is prime, and 2^4421 - 1, with a prime exponent, is not.
        [InlineData("(2^4423 - 1) in PP", "True")]
        [InlineData("(2^4421 - 1) in PP", "False")]
        // The least strong pseudoprime to the first twelve prime bases, which 41 exposes.
        [InlineData("318665857834031151167461 in PP", "False")]
        // Carmichael numbers and strong pseudoprimes to base 2.
        [InlineData("561 in PP", "False")]
        [InlineData("41041 in PP", "False")]
        [InlineData("2047 in PP", "False")]
        [InlineData("3215031751 in PP", "False")]
        // Past the exact range a witness still proves a number composite.
        [InlineData("(10^30 + 1) in PP", "False")]
        [InlineData("(10^30 + 3) in PP", "False")]
        public void Decided(string input, string expected)
        {
            var task = Task.Run(() => input.ToEntity().Evaled);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), $"{input} did not finish");
            Assert.Equal(expected.ToEntity(), task.Result);
        }

        /// <summary>
        /// Past the exact range, a number that passes every round is left open: <c>10^30 + 57</c> is prime, and
        /// <c>3317044064679887385961981</c>, the least strong pseudoprime to all thirteen bases, is not.
        /// </summary>
        [Theory]
        [InlineData("(10^30 + 57) in PP")]
        [InlineData("3317044064679887385961981 in PP")]
        public void LeftOpen(string input)
            => Assert.IsType<Set.Inf>(input.ToEntity().Evaled);

        [Fact]
        public void BelowTwoHundredThousandItIsTheSieve()
        {
            const int limit = 200000;
            var composite = new bool[limit];
            for (var i = 2; i * i < limit; i++)
                if (!composite[i])
                    for (var j = i * i; j < limit; j += i)
                        composite[j] = true;
            for (var n = 0; n < limit; n++)
                Assert.True(AngouriMath.Functions.Primes.IsPrime(EInteger.FromInt32(n)) == (n >= 2 && !composite[n]), $"{n}");
        }

        [Fact]
        public void BelowTenToTheTwelveItIsTrialDivision()
        {
            var random = new Random(2230);
            for (var t = 0; t < 2000; t++)
            {
                var n = (long)(random.NextDouble() * 1e12) | 1;
                var prime = n >= 2;
                for (long d = 3; d * d <= n && prime; d += 2)
                    prime = n % d != 0;
                Assert.True(AngouriMath.Functions.Primes.IsPrime(EInteger.FromInt64(n)) == prime, $"{n}");
            }
        }
    }
}
