//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// At a finite point l'Hopital's rule reads the quotient it gets term by term, as a value or a pole, and now reads a
    /// factor that is a sum or vanishes there by its leading order: ln(cos(x) * sqrt(x)) / (1 / (e^(2x) - 1)) steps to
    /// a quotient with (x^(-1/2) * cos(x) / 2 - sqrt(x) * sin(x)) and (e^(2x) - 1)^2 in it, of orders -1/2 and 2. It also
    /// reads it before another step, rather than only once the step has grown. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AVanishingFactorIsReadByItsOrderTest
    {
        [Theory]
        [InlineData("(e^(2*x) - 1) * ln(cos(x) * sqrt(x))", "0")]
        [InlineData("tanh(x) * ln(cos(x) * sqrt(x))", "0")]
        [InlineData("sinh(x) * ln(cos(x) * sqrt(x))", "0")]
        [InlineData("(cos(x) * sqrt(x)) ^ tanh(x)", "1")]
        [InlineData("(cos(x) * sqrt(x)) ^ arctan(tanh(x))", "1")]
        public void TheLimitIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);

        [Theory]
        [InlineData("(sin(x) - x) / x^3", "-1/6")]
        [InlineData("1 / x^2 - 1 / sin(x)^2", "-1/3")]
        [InlineData("(cosh(x) - 1) / (x * tanh(x))", "1/2")]
        [InlineData("tanh(x) * ln(cos(x) * x^2)", "0")]
        public void TheLimitStillHolds(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.BothSides).Evaled);
    }
}
