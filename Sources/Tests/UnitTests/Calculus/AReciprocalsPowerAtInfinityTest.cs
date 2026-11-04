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
    /// A one-sided limit at 0 is taken at infinity by putting <c>1/x</c> for <c>x</c>, so a root of the variable arrives
    /// there as <c>(1/x)^p</c>, which nothing rewrote. <c>x^3 * sqrt(1/x) / (x^2 * (1/x)^(-1/2))</c>, which is 1, then went
    /// to l'Hopital's rule sixteen deep, and <c>ln(x) / ln(sqrt(x))</c> from the right at 0 asked it eight times on the way
    /// to 2: 13 s, and <c>ln(sin(x)) / ln(sqrt(x))</c> over 20 s. Found by limitcheck at <c>DEPTH=3</c>,
    /// <c>SEED=26</c>, as <c>log(sqrt(x), floor(x) + arctan(x))</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AReciprocalsPowerAtInfinityTest
    {
        // The cap is far from both: each takes under 0.2 s, and took 13 to over 20 s.
        [Theory]
        [InlineData("ln(x) / ln(sqrt(x))", "0", ApproachFrom.Right, "2")]
        [InlineData("ln(sin(x)) / ln(sqrt(x))", "0", ApproachFrom.Right, "2")]
        [InlineData("log(sqrt(x), floor(x) + arctan(x))", "0", ApproachFrom.Right, "2")]
        [InlineData("ln(1/x) / ln(sqrt(1/x))", "+oo", ApproachFrom.BothSides, "2")]
        public void IsAnsweredPromptly(string input, string destination, ApproachFrom side, string expected)
        {
            var task = Task.Factory.StartNew(() => MathS.FromString(input).Limit("x", destination, side),
                TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(5)), $"lim {side} x->{destination} {input} did not finish in five seconds");
            Assert.Equal(MathS.FromString(expected), task.Result);
        }

        // The reciprocal's power from the left side, which the substitution writes as (-1/x)^p: (-1)^p x^(-p) for x > 0.
        [Theory]
        [InlineData("sqrt(-x) / x", "0", ApproachFrom.Left, "-oo")]
        [InlineData("sqrt(x) * ln(x)", "0", ApproachFrom.Right, "0")]
        [InlineData("x^(1/3) / sqrt(x)", "0", ApproachFrom.Right, "+oo")]
        public void IsTheLimit(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));
    }
}
