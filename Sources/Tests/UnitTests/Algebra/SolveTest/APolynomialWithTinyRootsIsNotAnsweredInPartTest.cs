//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// <c>2 + 3x + (-3 - x) x sgn(1 - x) = 0</c> with <c>x</c> written as <c>10^12 x</c> has the root
    /// <c>-sqrt(2) 10^-12</c>, and was answered <c>{ }</c>. Its parts solve to a degree-twelve
    /// polynomial with roots near 1e-12, whose numerical candidates all but one failed the check;
    /// the one exact root, <c>10^-12</c>, was answered as all of them, and the system solver above it
    /// took that root, a zero of the signum, and left nothing. A candidate that fails a polynomial as
    /// written is the solver's error, and the answer is left unsolved. Found by solving generated
    /// equations with <c>x</c> written as <c>10^12 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class APolynomialWithTinyRootsIsNotAnsweredInPartTest
    {
        [Fact]
        public void TheScaledSignumEquationIsNotEmpty()
        {
            var answer = "2 + 3 * (10^12 * x) + (-3 - 10^12 * x) * (10^12 * x) * sgn(1 - 10^12 * x) = 0".ToEntity().Solve("x");
            Assert.False(answer is FiniteSet { Count: 0 }, $"answered {answer}, but -sqrt(2) 10^-12 is a root");
        }

        [Fact]
        public void TheUnscaledEquationIsAsBefore()
            => Assert.IsType<ConditionalSet>("2 + 3x + (-3 - x) * x * sgn(1 - x) = 0".ToEntity().Solve("x"));
    }
}
