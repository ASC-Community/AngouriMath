//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// l'Hopital's rule was turned away where a quotient grew in nodes, and a chain whose divisor is a reciprocal power,
    /// <c>x^n / (1 / g^k)</c>, grows in its exponents instead: the derivative of <c>1/g^k</c> is over <c>g^(2k)</c>, so k
    /// went 6, 12, 24, ..., and the limit of <c>(-ln(2)/ln(x))^(e^x - 1)</c> at 0 from the right did not finish in ten
    /// minutes. It is 1, as 2.6.0 answered; the spelling with <c>log(x, 1/2)</c> and <c>sgn(x)</c>, 2.6.0 answered 0. Found by
    /// limitcheck at <c>DEPTH=3</c>, <c>SEED=23</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AQuotientWhosePowersGrowIsNotDifferentiatedTest
    {
        [Theory]
        [InlineData("(-ln(2)/ln(x))^(exp(x) - 1)")]
        [InlineData("(log(x, 1/2))^(exp(x) - sgn(x))")]
        public void IsOneAtZeroFromTheRight(string input)
        {
            var task = Task.Factory.StartNew(() => MathS.FromString(input).Limit("x", 0, ApproachFrom.Right),
                TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(60)), $"the limit of {input} at 0 from the right did not finish in a minute");
            Assert.Equal(Number.Integer.One, task.Result);
        }

        [Theory]
        [InlineData("1/x^2 - 1/sin(x)^2", "-1/3")]
        [InlineData("(1 - cos(x)) / x^2", "1/2")]
        [InlineData("x^3 / (x - sin(x))", "6")]
        public void AChainThatGetsSomewhereIsAsBefore(string input, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", 0));
    }
}
