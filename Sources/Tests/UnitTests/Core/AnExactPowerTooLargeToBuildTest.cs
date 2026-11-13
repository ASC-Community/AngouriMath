//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// An exact number to a whole power was built exactly however large, so <c>46655^(10^20)</c>, which has
    /// 4.7e20 digits, neither returned nor stopped when cancelled, and nor did a simplification of anything
    /// holding it. Found by compilesweep, on <c>(x^x - 1)^(e^(e^x))</c> at 6. Past about sixteen million bits
    /// the power is left written, evaluated or simplified: it is finite and not zero, and as the decimal it was
    /// <c>+oo</c> or <c>0</c>, which made <c>2^(10^9) - 2^(10^9)</c> NaN and <c>2^(10^9) &gt; 2^(10^9 - 1)</c>
    /// False. What is decided of it is decided by structure, by its sign or by its exponent.
    /// https://github.com/asc-community/AngouriMath/issues/1448
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AnExactPowerTooLargeToBuildTest
    {
        [Theory]
        [InlineData("46655^(10^20)")]
        [InlineData("(-3)^(10^20 + 1)")]
        [InlineData("(-3)^(10^20)")]
        [InlineData("(5/2)^(10^7)")]
        [InlineData("2^(10^9)")]
        [InlineData("2^(-(10^9))")]
        [InlineData("8^(10^9/3)")]
        [InlineData("46655^(e^(e^6))")]
        [InlineData("(1 + i)^(10^9)")]
        public void IsLeftWritten(string input)
        {
            var evaluated = Within(() => input.ToEntity().Evaled, input);
            Assert.True(evaluated is Powf, $"{input} was evaluated to {evaluated}");
        }

        [Theory]
        // Each of -1, i and -i keeps its exact power, however large.
        [InlineData("i^(10^20)", "1")]
        [InlineData("(-1)^(10^20 + 1)", "-1")]
        [InlineData("(-i)^(10^20 + 3)", "i")]
        // What holds the power is settled where structure, sign or exponent settle it.
        [InlineData("0 * 2^(10^9)", "0")]
        [InlineData("2^(10^9) > 2^(10^9 - 1)", "true")]
        [InlineData("2^(10^9) < 2^(10^9 - 1)", "false")]
        [InlineData("(1/2)^(10^9) > (1/2)^(10^9 - 1)", "false")]
        [InlineData("2^(-(10^9)) > 0", "true")]
        [InlineData("2^(10^9) <= 0", "false")]
        [InlineData("(-3)^(10^20 + 1) < 0", "true")]
        [InlineData("(-3)^(10^20 + 1) >= 5", "false")]
        [InlineData("2^(10^9) = 2^(10^9)", "true")]
        [InlineData("2^(10^9) = 2^(10^9 - 1)", "false")]
        [InlineData("2^(10^9) = 0", "false")]
        // Otherwise by the logarithms of the magnitudes, where they differ by more than their error.
        [InlineData("2^(10^9) > 5", "true")]
        [InlineData("3^(10^9) > 2^(10^9)", "true")]
        [InlineData("2^(10^9) > 10^(10^8)", "true")]
        [InlineData("2^(-(10^9)) < 10^(-100)", "true")]
        [InlineData("(-3)^(10^20 + 1) < -5", "true")]
        [InlineData("(1 + 2^(-40))^(10^6) > 1", "true")]
        public void IsEvaluated(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, Within(() => input.ToEntity().Evaled, input));

        [Theory]
        [InlineData("2^(10^9) - 2^(10^9)", "0")]
        [InlineData("x - x + 2^(10^9) - 2^(10^9)", "0")]
        [InlineData("2^(10^9) / 2^(10^9 - 1)", "2")]
        [InlineData("2^(10^9) * 2^(-(10^9))", "1")]
        [InlineData("2^(-(10^9)) > 0", "true")]
        [InlineData("2^(10^9)", "2^(10^9)")]
        public void IsSimplified(string input, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, Within(() => input.ToEntity().Simplify(), input));

        /// <summary>
        /// Two magnitudes that may be equal are not told apart, and two that differ by less than any rounding are
        /// not compared through it: as the decimals, the first was True only because both sides were <c>+oo</c>.
        /// </summary>
        [Theory]
        [InlineData("2^(10^9) = 4^(5 * 10^8)")]
        [InlineData("2^(10^9) > 2^(10^9) - 1")]
        public void IsLeftUndecided(string input)
        {
            var evaluated = Within(() => input.ToEntity().Evaled, input);
            Assert.True(evaluated is not Entity.Boolean, $"{input} was decided as {evaluated}");
        }

        /// <summary>A witness search that meets the power moves on, rather than hanging on it.</summary>
        [Fact]
        public void AQuantifierOverATowerReturns()
        {
            const string input = "forall n in ZZ* : 3^(n + 1) divides 2^(3^n) + 1";
            Assert.Equal(input.ToEntity(), Within(() => input.ToEntity().Evaled, input));
        }

        [Theory]
        [InlineData("2^(10^10)")]
        [InlineData("46655^(10^20) + 1")]
        [InlineData("(1/3)^(10^20)")]
        public void AnExactSimplificationLeavesItAsWritten(string input)
        {
            var simplified = Within(() => input.ToEntity().InnerSimplified, input);
            Assert.True(simplified is not Number, $"{input} was simplified to {simplified}");
            Assert.True(Within(() => input.ToEntity().Simplify(), input) is not Number, $"{input} was simplified to a number");
        }

        [Fact]
        public void APowerBelowTheBoundIsStillExact()
            => Assert.Equal(Integer.Create(PeterO.Numbers.EInteger.One.ShiftLeft(1000000)), Within(() => "2^(10^6)".ToEntity().Evaled, "2^(10^6)"));

        private static Entity Within(Func<Entity> evaluation, string input)
        {
            var task = Task.Factory.StartNew(evaluation, TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), $"{input} did not finish in thirty seconds");
            return task.Result;
        }
    }
}
