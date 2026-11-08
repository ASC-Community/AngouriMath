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

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// From one side, a factor tending to 0 times a sum tending to an infinity is asked term by term.
    /// Read as a product, l'Hopital's rule took <c>x * (ln(-tanh(x)) + 1)</c> at 0 from the left through
    /// four and a half seconds of nested limits, where <c>x ln(-tanh(x)) + x</c> is 0 at once; and
    /// <c>(tanh(x)^3)^exp(1/x)</c> came to the same product. Found in the limitcheck A/B of the
    /// logarithm-of-a-power change; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AVanishingFactorTimesASumTest
    {
        [Theory]
        [InlineData("x * (ln(-tanh(x)) + 1)", "0")]
        [InlineData("x * (ln(-tanh(x)) - 1)", "0")]
        [InlineData("e^(1/x) * (3 * ln(-tanh(x)) + 1)", "0")]
        [InlineData("(tanh(x)^3)^exp(1/x)", "1")]
        [InlineData("sin(x) * (ln(-tanh(x)) + 1/x)", "1")]
        public void IsAnsweredFromTheLeft(string input, string expected)
        {
            var limit = Task.Run(() => MathS.FromString(input).Limit("x", 0, ApproachFrom.Left));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(20)), $"{input} did not finish");
            Assert.Equal(MathS.FromString(expected), limit.Result);
        }
    }
}
