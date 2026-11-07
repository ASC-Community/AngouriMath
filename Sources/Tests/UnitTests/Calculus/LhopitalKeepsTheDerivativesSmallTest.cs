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
    /// l'Hopital's rule differentiates the whole of each side, and the product rule carries every
    /// factor beside the one it differentiates. A factor that settles at a number other than 0, or a
    /// polynomial written on both sides, only makes the derivatives larger, and with tanh written
    /// through exponentials they did not finish: <c>ln(x + tanh(x)) / ln((x - pi)^2)</c> at +oo,
    /// which is 1/2, was still running after a minute. Found by limitcheck at <c>DEPTH=3</c>; no
    /// issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LhopitalKeepsTheDerivativesSmallTest
    {
        [Theory]
        [InlineData("ln(tanh(x) + x) / ln((x - pi)^2)", "+oo", "1/2")]
        [InlineData("ln(tanh(x) + x) / ln(x - pi)", "+oo", "1")]
        [InlineData("ln(tanh(x) + x) / ln(x)", "+oo", "1")]
        [InlineData("ln(arctan(x) + x) / ln(x)", "+oo", "1")]
        [InlineData("tanh(x) * x / (x + tanh(x))", "-oo", "-1")]
        [InlineData("tanh(x) * x / (x + tanh(x))", "+oo", "1")]
        [InlineData("(x - pi)^2 / ((x + tanh(x)) * (x - pi))", "+oo", "1")]
        [InlineData("arctan(x) * x / (x + 1)", "+oo", "pi / 2")]
        [InlineData("(x - 1)^2 * sin(x - 1) / ((x - 1)^3 * cos(x))", "1", "sec(1)")]
        public void Finishes(string input, string destination, string expected)
        {
            var limit = Task.Run(() => MathS.FromString(input).Limit("x", destination).Simplify());
            Assert.True(limit.Wait(TimeSpan.FromSeconds(60)), $"{input} at {destination} did not finish");
            Assert.Equal(MathS.FromString(expected).Simplify(), limit.Result);
        }

        // A factor that is 0 everywhere is not cancelled: 0 / 0 is not 1.
        [Fact]
        public void DoesNotCancelAFactorThatIsZeroEverywhere()
            => Assert.Equal(MathS.NaN, MathS.FromString("(x - x) * sin(x) / ((x - x) * x)").Limit("x", 0));
    }
}
