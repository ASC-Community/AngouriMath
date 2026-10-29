//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The binomial coefficient <c>binomial(n, k)</c> as a node: computed for whole arguments
    /// by the falling factorial, kept as itself for symbolic ones.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1409">#1409</a>,
    /// <a href="https://github.com/asc-community/AngouriMath/issues/809">#809</a>
    /// </summary>
    [Trait("Area", "Discrete")]
    public sealed class BinomialTest
    {
        [Theory]
        [InlineData("binomial(5, 2)", "10")]
        [InlineData("binomial(5, 0)", "1")]
        [InlineData("binomial(5, 5)", "1")]
        [InlineData("binomial(0, 0)", "1")]
        [InlineData("binomial(100, 50)", "100891344545564193334812497256")]
        // Above n, and below zero, there is nothing to choose.
        [InlineData("binomial(5, 6)", "0")]
        [InlineData("binomial(5, -1)", "0")]
        // A negative n is the falling factorial: (-1)(-2)(-3)/3! and (-2)(-3)(-4)/3!.
        [InlineData("binomial(-1, 3)", "-1")]
        [InlineData("binomial(-2, 3)", "-4")]
        // And so is a rational one: (1/2)(-1/2)/2!.
        [InlineData("binomial(1/2, 2)", "-1/8")]
        // n choose 0 is the empty product whatever n is.
        [InlineData("binomial(n, 0)", "1")]
        [InlineData("binomial(4, 2) + 1", "7")]
        public void WholeLowerArgumentsAreComputed(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        /// <summary>
        /// The named form is what Pascal's identity and the binomial theorem read, so a
        /// symbolic argument keeps the node rather than becoming a ratio of factorials.
        /// </summary>
        [Theory]
        [InlineData("binomial(n, k)")]
        [InlineData("binomial(n, 2)")]
        [InlineData("binomial(x, 1/2)")]
        public void ASymbolicArgumentKeepsTheNode(string input)
            => Assert.IsType<Entity.Binomialf>(input.ToEntity().Simplify());

        [Fact]
        public void SubstitutingTheUpperArgumentComputesIt()
            => Assert.Equal("10".ToEntity(), "binomial(n, 2)".ToEntity().Substitute("n", 5).Simplify());

        /// <summary>
        /// A lower argument that is not whole goes through the gamma function, numerically:
        /// <c>binomial(5/2, 3/2)</c> is <c>Γ(7/2)/(Γ(5/2) Γ(2))</c>, which is <c>5/2</c>.
        /// </summary>
        [Fact]
        public void AFractionalLowerArgumentEvaluatesThroughTheGammaFunction()
        {
            var value = "binomial(5/2, 3/2)".ToEntity().EvalNumerical();
            Assert.True(Math.Abs((double)value.RealPart - 2.5) < 1e-9, $"binomial(5/2, 3/2) evaluated to {value}");
        }

        /// <summary>
        /// A whole upper argument with a lower one that is not whole goes through the gamma function
        /// as well, against mpmath to 25 digits: <c>binomial(3, 1/2)</c> is <c>32/(5 pi)</c>. It was
        /// left unevaluated. With a negative whole upper argument there is no value.
        /// </summary>
        [Theory]
        [InlineData("binomial(3, 1/2)", "2.03718327157626029784171217117")]
        [InlineData("binomial(0, 1/2)", "0.63661977236758134307553505349")]
        [InlineData("binomial(4, 3/2)", "5.43248872420336079424456578978")]
        [InlineData("binomial(3, -1/2)", "0.291026181653751471120244595881")]
        public void AWholeUpperArgumentWithAFractionalLowerOneEvaluatesThroughTheGammaFunction(string input, string expected)
        {
            var value = Assert.IsAssignableFrom<Entity.Number.Real>(input.ToEntity().EvalNumerical());
            Assert.True(value.EDecimal.Subtract(PeterO.Numbers.EDecimal.FromString(expected)).Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1e-25")) < 0, $"{input} evaluated to {value}");
        }

        [Fact]
        public void ANegativeWholeUpperArgumentWithAFractionalLowerOneHasNoValue()
            => Assert.Equal(MathS.NaN, "binomial(-3, 1/2)".ToEntity().EvalNumerical());

        /// <summary>
        /// Every whole k has a value, whatever n is; a k that is not whole has one unless n is
        /// a negative whole number, where <c>Γ(n + 1)</c> has a pole.
        /// </summary>
        [Fact]
        public void TheDomainConditionIsTheGammaFunctions()
        {
            Assert.Equal("k in ZZ or not (n in ZZ and n < 0)".ToEntity(), "binomial(n, k)".ToEntity().DomainCondition);
            Assert.Equal("True".ToEntity(), "binomial(n, 3)".ToEntity().DomainCondition.Simplify());
        }

        [Theory]
        [InlineData("binomial(n, k)", "binomial(n, k)")]
        [InlineData("binomial(n + 1, k) - binomial(n, k)", "binomial(n + 1, k) - binomial(n, k)")]
        public void ThePrintedFormIsTheUsualSpelling(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Stringize());

        [Theory]
        [InlineData("binomial(n, k)")]
        [InlineData("binomial(n, k) + binomial(n, k + 1)")]
        public void ThePrintedFormParsesBackToTheSameExpression(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().Stringize().ToEntity());

        [Fact]
        public void TheLatexIsBinom()
            => Assert.Equal(@"\binom{n}{k}", "binomial(n, k)".ToEntity().Latexize());

        [Fact]
        public void TheSympyCodeIsSympyBinomial()
            => Assert.Contains("sympy.binomial(n, k)", MathS.ToSympyCode("binomial(n, k)".ToEntity()));

        [Fact]
        public void TheEntryPointIsTheNode()
            => Assert.Equal("binomial(n, k)".ToEntity(), MathS.Binomial("n", "k"));
    }
}
