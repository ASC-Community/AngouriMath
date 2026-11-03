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
    /// holding it. Past about sixteen million bits the power is now evaluated as the decimal, as the same
    /// power with a half added already was: <c>+oo</c> past the context's range. An exact simplification
    /// leaves it as written, since the infinity is the decimal's and not the power's. Found by compilesweep,
    /// on <c>(x^x - 1)^(e^(e^x))</c> at 6; no issue was filed.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AnExactPowerTooLargeToBuildTest
    {
        [Theory]
        [InlineData("46655^(10^20)", "+oo")]
        [InlineData("46655^(e^(e^6))", "+oo")]
        [InlineData("(-3)^(10^20 + 1)", "-oo")]
        [InlineData("(-3)^(10^20)", "+oo")]
        [InlineData("(5/2)^(10^7)", "+oo")]
        // Each of -1, i and -i keeps its exact power, however large.
        [InlineData("i^(10^20)", "1")]
        [InlineData("(-1)^(10^20 + 1)", "-1")]
        [InlineData("(-i)^(10^20 + 3)", "i")]
        public void IsEvaluated(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, Within(() => input.ToEntity().Evaled, input));

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
