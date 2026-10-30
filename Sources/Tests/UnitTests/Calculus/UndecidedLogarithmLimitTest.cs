//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit of a logarithm that is not decided is left unevaluated, where it overflowed the stack. A limit node simplifies
    /// its expression evaluated, in which ln(f) is log(2.718..., f), the base was not read as e, and the quotient
    /// ln(f) / ln(2.718...) it was rewritten as asked the limit of ln(f) again through a limit node of its own. None of these
    /// has a limit, as the argument oscillates, so NaN would be right as well.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2273">#2273</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class UndecidedLogarithmLimitTest
    {
        [Theory]
        [InlineData("limitright(ln(sin(1/x) + 2), x, 0)")]
        [InlineData("limit(ln(sin(1/x) + 2), x, 0)")]
        [InlineData("limitleft(ln(2 + cos(1/x)), x, 0)")]
        [InlineData("limit(ln(2 + sin(x)), x, +oo)")]
        [InlineData("limit(sqrt(x) * ln(2 + sin(1/x)), x, 0)")]
        public void ItIsLeftUnevaluated(string limit)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(answer is Entity.Limitf || answer == MathS.NaN, $"{limit} is {answer}");
        }

        /// <summary>A logarithm whose limit is decided is answered as before.</summary>
        [Theory]
        [InlineData("limit(ln(x) / ln(2), x, 4)", "2")]
        [InlineData("limit(ln(1 + x) / x, x, 0)", "1")]
        [InlineData("limitright(ln(x) * x, x, 0)", "0")]
        public void ADecidedOneIsAnswered(string limit, string value)
            => Assert.Equal(value.ToEntity(), limit.ToEntity().Simplify());
    }
}
