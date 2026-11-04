//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// l'Hopital's rule read a part whose limit nobody could take as infinite, because the unevaluated
    /// <c>limit(sin(u), x, +oo)</c> holds a <c>+oo</c> -- its destination. <c>sin(u) / cos(u)</c> with both limits
    /// undecided was taken for oo/oo and differentiated, and <c>tan(x^2 + x - 1)</c> at 0 from the left answered
    /// <c>-cos(-1) / sin(-1)</c>, which is -cot(-1) = 0.642, where the limit is tan(-1) = -1.557. The same on 2.6.0.
    /// Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=26</c>; no issue was filed. And a side asked for at an infinity
    /// is now the limit there, which the Stirling limits were reading through the same mistake.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnUndecidedLimitIsNotInfiniteTest
    {
        [Theory]
        [InlineData("tan(x^2 + x - 1)", ApproachFrom.Left)]
        [InlineData("sin(x^2 + x - 1) / cos(x^2 + x - 1)", ApproachFrom.Left)]
        [InlineData("tan(x^2 + x - 1)", ApproachFrom.Right)]
        public void IsTheTangent(string input, ApproachFrom side)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            Assert.False(limit is Limitf, $"lim {side} x->0 {input} was left unevaluated");
            AssertIsTheTangent(input, side, limit);
        }

        // These come back unevaluated: the parts' own limits from the left are not taken, and that is a decline, not a
        // wrong answer. What they must not be is the quotient of the derivatives, as the first was before.
        [Theory]
        [InlineData("tan(x^2 - x - 1)", ApproachFrom.Left)]
        [InlineData("tan(x^2 + x - 1)", ApproachFrom.BothSides)]
        public void IsTheTangentOrUnevaluated(string input, ApproachFrom side)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            if (limit is not Limitf)
                AssertIsTheTangent(input, side, limit);
        }

        // An infinity has one side, and asking for it one-sidedly was left unevaluated where the limit is a value. The
        // Stirling limits relied on reading the first of these, unevaluated, as infinite: it is -oo, by luck. The last
        // two are a power whose base tends to a number and whose exponent tends to 0.
        [Theory]
        [InlineData("-x + ln(2*pi*x)/2", "+oo", ApproachFrom.Left, "-oo")]
        [InlineData("e^x * x", "-oo", ApproachFrom.Right, "0")]
        [InlineData("x - sqrt(x^2 + x)", "+oo", ApproachFrom.Left, "-1/2")]
        [InlineData("x - sqrt(x^2 + x)", "+oo", ApproachFrom.Right, "-1/2")]
        // Answered only while its base, asked at +oo from the left, came back undecided and was taken for infinite.
        // Decided, the base tends to 1 and the power to 0, which is 1 and not an indeterminate form.
        [InlineData("log(x, tanh(x)) ^ (x ^ (pi + x))", "0", ApproachFrom.Right, "1")]
        [InlineData("(x - 3) ^ x", "0", ApproachFrom.Left, "1")]
        public void IsTheLimit(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));

        private static void AssertIsTheTangent(string input, ApproachFrom side, Entity limit)
        {
            var difference = (limit - MathS.Tan(-1)).EvalNumerical();
            Assert.True(difference is Complex value && Complex.Abs(value) is Real abs && abs < 1e-10,
                $"lim {side} x->0 {input} = {limit}, which is not tan(-1)");
        }
    }
}
