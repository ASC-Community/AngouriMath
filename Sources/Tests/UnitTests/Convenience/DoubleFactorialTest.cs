//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The double factorial is the postfix <c>n!!</c>, as mathematics writes it and as the factorial
    /// is the postfix <c>n!</c>. <c>5!!</c> was a parse error.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class DoubleFactorialTest
    {
        [Fact]
        public void TwoMarksAreTheDoubleFactorialAndNotTheFactorialTwice()
        {
            Assert.Equal(new DoubleFactorialf("n"), "n!!".ToEntity());
            Assert.Equal(new Factorialf(new Factorialf("n")), "(n!)!".ToEntity());
            Assert.Equal(new Factorialf(new DoubleFactorialf("n")), "(n!!)!".ToEntity());
            Assert.Equal(new DoubleFactorialf("x + 1".ToEntity()), "(x + 1)!!".ToEntity());
        }

        /// <summary>Three marks are the triple factorial in the literature, refused rather than read as <c>(n!!)!</c>.</summary>
        [Fact]
        public void ThreeMarksAreRefused()
            => Assert.Throws<UnrecognizedFunctionParseException>(() => "n!!!".ToEntity());

        /// <summary>
        /// The product of the positive integers up to <c>n</c> with its parity; <c>0!!</c> and
        /// <c>(-1)!!</c> are the empty product, and below them <c>n!! = (n + 2)!!/(n + 2)</c>.
        /// </summary>
        [Theory]
        [InlineData("0!!", "1")]
        [InlineData("1!!", "1")]
        [InlineData("2!!", "2")]
        [InlineData("7!!", "105")]
        [InlineData("8!!", "384")]
        [InlineData("(-1)!!", "1")]
        [InlineData("(-3)!!", "-1")]
        [InlineData("(-5)!!", "1/3")]
        [InlineData("(-7)!!", "-1/15")]
        public void ItIsComputedOnTheIntegers(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>
        /// A negative even integer has none, and nothing off the integers has one: the extension
        /// through the gamma function does not give <c>0!! = 1</c>.
        /// </summary>
        [Theory]
        [InlineData("(-2)!!")]
        [InlineData("(-4)!!")]
        [InlineData("(5/2)!!")]
        [InlineData("i!!")]
        public void ItHasNoValueOffItsDomain(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().InnerSimplified);

        /// <summary><c>n! = n!! (n - 1)!!</c>.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(9)]
        [InlineData(20)]
        public void TheFactorialIsTheProductOfTwoNeighbours(int n)
            => Assert.Equal(new Factorialf(n).Evaled, (new DoubleFactorialf(n) * new DoubleFactorialf(n - 1)).InnerSimplified);

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<DoubleFactorialf>("n!!".ToEntity().InnerSimplified);

        [Theory]
        [InlineData("n!!", "n!!", "n!!", "sympy.factorial2(n)")]
        [InlineData("(x + 1)!!", "(x + 1)!!", @"\left(x+1\right)!!", "sympy.factorial2(x + 1)")]
        public void ItIsWrittenAsTwoMarks(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
