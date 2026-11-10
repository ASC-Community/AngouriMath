//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// l'Hopital's rule read a product of a power of the variable and a vanishing factor as the power over the
    /// reciprocal of that factor, which differentiating makes worse on every step: x^2 * ln(1 - 1 / x) at +oo went to
    /// x^3 * ln(1 - 1 / x)^2, then x^4 * ln(1 - 1 / x)^3, for three seconds, and (-1 - x) / log(x, 2)^(x - 1) at 1
    /// from the left reaches it and took six. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerOfTheVariableIsInvertedTest
    {
        [Theory]
        [InlineData("x^2 * ln(1 - 1 / x)", "+oo", ApproachFrom.BothSides, "-oo")]
        [InlineData("ln(1 + 1 / x) * ln(1 / x)", "+oo", ApproachFrom.BothSides, "0")]
        [InlineData("(x - 1) * ln(ln(2) / ln(x))", "1", ApproachFrom.Left, "0")]
        [InlineData("(ln(2) / ln(x))^(x - 1)", "1", ApproachFrom.Left, "1")]
        public void TheProductFinishes(string input, string at, ApproachFrom side, string expected)
        {
            Entity? limit = null;
            var task = Task.Factory.StartNew(() => limit = input.ToEntity().Limit("x", at, side), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "the limit did not finish in 30 seconds");
            Assert.Equal(expected.ToEntity(), limit);
        }

        // Products of a power and a vanishing factor, at a finite point and at infinity, and the remarkable limits.
        [Theory]
        [InlineData("x * ln(1 + 1 / x)", "+oo", "1")]
        [InlineData("x * log(2, 1 + 1 / x)", "+oo", "1 / ln(2)")]
        [InlineData("x^2 * (1 - cos(1 / x))", "+oo", "1/2")]
        [InlineData("x^4 * e^(-x)", "+oo", "0")]
        [InlineData("x^(-2) * (1 - cos(x))", "0", "1/2")]
        [InlineData("ln(1 + x) / x", "0", "1")]
        [InlineData("ln(x) / (x - 1)", "1", "1")]
        public void TheLimitStillHolds(string input, string at, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), input.ToEntity().Limit("x", at).Simplify());
    }
}
