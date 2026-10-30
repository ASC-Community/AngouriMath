//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit of a logarithm, or of a power to an exponent that is not whole, whose argument arrives on the negative real
    /// line other than along it is not read off the principal value there. The principal logarithm is continuous on its cut
    /// from above only: x^x comes to -1 from below as x comes down to -1, so limitright(ln(x^x), x, -1) is -i pi, and it
    /// answered ln(-1), which is i pi. Each expected value is the expression's near the destination.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2266">#2266</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LimitOnABranchCutTest
    {
        /// <summary>From below the cut, or from both sides of it, where the principal value is not the limit.</summary>
        [Theory]
        [InlineData("limitright(ln(x^x), x, -1)", "-i pi")]
        [InlineData("limitright(sqrt(x^x), x, -1)", "-i")]
        [InlineData("limitright(log(2, x^x), x, -1)", "-i pi / ln(2)")]
        [InlineData("limitright(ln(x^x) * x, x, -1)", "i pi")]
        [InlineData("limitleft(ln(-1 + i x), x, 0)", "-i pi")]
        [InlineData("limitleft(sqrt(-1 + i x), x, 0)", "-i")]
        [InlineData("limitleft(ln(-1 - i x^2), x, 0)", "-i pi")]
        [InlineData("limitright(ln(-e^(i x)), x, 0)", "-i pi")]
        public void ItIsTheLimitOrNothing(string limit, string value)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(IsUnevaluated(answer) || answer.Evaled == value.ToEntity().Evaled, $"{limit} is {answer}, where it is {value}");
        }

        /// <summary>Where the sides disagree there is no limit, and the principal value was given for it.</summary>
        [Theory]
        [InlineData("limit(ln(x^x), x, -1)")]
        [InlineData("limit(ln(-1 + i x), x, 0)")]
        [InlineData("limit(ln(e^(i x) - 2), x, pi)")]
        public void ThereIsNone(string limit)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(IsUnevaluated(answer) || answer == MathS.NaN, $"{limit} is {answer}");
        }

        /// <summary>A limit left as a limit, where ln(-1) is an answer like any number.</summary>
        private static bool IsUnevaluated(Entity answer) => answer.Nodes.Any(node => node is Entity.Limitf);

        /// <summary>Along the real line the value on the cut is the limit, and is answered as before.</summary>
        [Theory]
        [InlineData("limit(ln(x), x, -1)", "i pi")]
        [InlineData("limitright(ln(x), x, -1)", "i pi")]
        [InlineData("limit(ln(x - 2), x, 0)", "ln(-2)")]
        [InlineData("limit(sqrt(x - 2), x, 0)", "sqrt(-2)")]
        [InlineData("limit(ln(-1 + x^2), x, 0)", "i pi")]
        [InlineData("limit(x^(1/3), x, -8)", "-2")]
        [InlineData("limitright(x^x, x, -1)", "-1")]
        [InlineData("limitright(ln(x^x), x, -1/2)", "ln((-1/2)^(-1/2))")]
        [InlineData("limit(ln(sin(x) - 2), x, 0)", "ln(-2)")]
        public void AlongTheRealLineItIsTheValue(string limit, string value)
            => Assert.Equal(value.ToEntity().Evaled, limit.ToEntity().Simplify().Evaled);
    }
}
