//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The factorisation, the divisor count and the totient trial-divided over <c>long</c>: past 2^63 each threw
    /// <c>OverflowException</c>, so <c>phi(7^30)</c> threw out of <c>Simplify</c>; the loop was bounded by the running
    /// totient rather than what was left, so <c>2^62</c> took 1.5e9 divisions; and past 3037000499 the divisor's square
    /// wrapped negative, so the largest prime below 2^63 never finished. <c>IsPrime</c> went through the same loop.
    /// https://github.com/asc-community/AngouriMath/issues/2341,
    /// https://github.com/asc-community/AngouriMath/issues/2342
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AFactorisationPastTheLongsTest
    {
        [Theory]
        [InlineData("18446744073709551616", "2^64")]
        [InlineData("4611686018427387904", "2^62")]
        [InlineData("9223372036854775783", "9223372036854775783^1")]
        [InlineData("22539340290692258087863249", "7^30")]
        [InlineData("618970019642690137449562111", "618970019642690137449562111^1")]
        [InlineData("1000000016000000063", "1000000007^1 * 1000000009^1")]
        [InlineData("360", "2^3 * 3^2 * 5^1")]
        public void IsFactorised(string number, string expected)
        {
            var factors = Within(() => string.Join(" * ", MathS.NumberTheory.Factorize(Integer.Create(EInteger.FromString(number)))
                .Select(f => $"{f.prime}^{f.power}")));
            Assert.Equal(expected, factors);
        }

        [Theory]
        [InlineData("18446744073709551616", "65")]
        [InlineData("4611686018427387904", "63")]
        [InlineData("9223372036854775783", "2")]
        [InlineData("360", "24")]
        public void HasItsDivisorsCounted(string number, string expected)
            => Assert.Equal(Integer.Create(EInteger.FromString(expected)),
                Within(() => MathS.NumberTheory.CountDivisors(Integer.Create(EInteger.FromString(number)))));

        [Theory]
        [InlineData("phi(2^64)", "9223372036854775808")]
        [InlineData("phi(2^62)", "2305843009213693952")]
        [InlineData("phi(9223372036854775783)", "9223372036854775782")]
        [InlineData("phi(7^30)", "19319434534879078361025642")]
        [InlineData("phi(360)", "96")]
        public void HasItsTotient(string input, string expected)
        {
            Assert.Equal(expected.ToEntity(), Within(() => input.ToEntity().Evaled));
            Assert.Equal(expected.ToEntity(), Within(() => input.ToEntity().Simplify()));
        }

        [Theory]
        [InlineData("2147483647", true)]
        [InlineData("2305843009213693951", true)]
        [InlineData("618970019642690137449562111", true)]
        [InlineData("9223372036854775783", true)]
        [InlineData("9223372036854775781", false)]
        [InlineData("1", false)]
        [InlineData("-7", false)]
        public void IsDecidedPrime(string number, bool expected)
            => Assert.Equal(expected, Within(() => Integer.Create(EInteger.FromString(number)).IsPrime));

        [Fact]
        public void APowerOfALargePrimeHasItsTotient()
        {
            Assert.Equal("(2 ^ 89 - 1) ^ (k - 1) * (2 ^ 89 - 2)".ToEntity().Simplify(),
                Within(() => "phi((2^89 - 1)^k)".ToEntity().Simplify()));
        }

        private static T Within<T>(Func<T> evaluation)
        {
            var task = Task.Factory.StartNew(evaluation, TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "did not finish in thirty seconds");
            return task.Result;
        }
    }
}
