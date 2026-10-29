//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The falling and the rising factorial, x to the n falling and rising, in the notation of
    /// Graham, Knuth and Patashnik's <i>Concrete Mathematics</i>. Both names were refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class FactorialPowerTest
    {
        [Theory]
        [InlineData("fallingfactorial(5, 2)", "20")]
        [InlineData("fallingfactorial(5, 0)", "1")]
        [InlineData("fallingfactorial(x, 0)", "1")]
        [InlineData("fallingfactorial(5, 5)", "120")]
        [InlineData("fallingfactorial(5, 6)", "0")]
        [InlineData("fallingfactorial(1/2, 2)", "-1/4")]
        [InlineData("fallingfactorial(5, -2)", "1/42")]
        [InlineData("fallingfactorial(-3, -2)", "1/2")]
        [InlineData("factorialpower(7, 3)", "210")]
        [InlineData("risingfactorial(3, 2)", "12")]
        [InlineData("risingfactorial(3, 0)", "1")]
        [InlineData("risingfactorial(3, -2)", "1/2")]
        [InlineData("risingfactorial(0, 3)", "0")]
        [InlineData("risingfactorial(-2, 3)", "0")]
        [InlineData("risingfactorial(1/2, 3)", "15/8")]
        public void TheyAreProductsForAWholeOrder(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>
        /// For a negative order a factor of the reciprocal vanishes, and for any other the numerator
        /// of the gamma ratio has a pole: fallingfactorial(-2, 1/2) is Γ(-1)/Γ(-3/2).
        /// </summary>
        [Theory]
        [InlineData("fallingfactorial(-1, -1)")]
        [InlineData("fallingfactorial(-2, -3)")]
        [InlineData("fallingfactorial(-2, 1/2)")]
        [InlineData("risingfactorial(1, -1)")]
        [InlineData("risingfactorial(2, -3)")]
        [InlineData("risingfactorial(-1/2, 1/2)")]
        public void TheyHaveNoValueAtAPole(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().InnerSimplified);

        /// <summary>
        /// At a pole of the denominator's gamma function the reciprocal vanishes:
        /// fallingfactorial(1/2, 3/2) is Γ(3/2)/Γ(0), and risingfactorial(0, 1/2) is Γ(1/2)/Γ(0).
        /// </summary>
        [Theory]
        [InlineData("fallingfactorial(1/2, 3/2)")]
        [InlineData("risingfactorial(0, 1/2)")]
        [InlineData("risingfactorial(-3, 5/2)")]
        public void TheyVanishAtAPoleOfTheDenominator(string written)
            => Assert.Equal(Integer.Zero, written.ToEntity().InnerSimplified);

        /// <summary>Γ(6)/Γ(11/2) and Γ(5/2)/Γ(2), against mpmath to 30 digits.</summary>
        [Theory]
        [InlineData("fallingfactorial(5, 1/2)", "2.29257989505120015013822761269")]
        [InlineData("risingfactorial(2, 1/2)", "1.32934038817913702047362561251")]
        public void ForAnyOtherOrderTheyAreAGammaRatio(string written, string expected)
        {
            var value = Assert.IsAssignableFrom<Real>(written.ToEntity().EvalNumerical());
            var difference = value.EDecimal.Subtract(EDecimal.FromString(expected)).Abs();
            Assert.True(difference.CompareTo(EDecimal.FromString("1e-20")) < 0, value.Stringize());
        }

        /// <summary>x to the n rising is x + n - 1 to the n falling.</summary>
        [Theory]
        [InlineData(-3)]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(4)]
        public void TheRisingFactorialIsAFallingOne(int n)
        {
            var x = Rational.Create(7, 3);
            Assert.Equal(
                MathS.FallingFactorial(x + n - 1, n).InnerSimplified,
                MathS.RisingFactorial(x, n).InnerSimplified);
        }

        [Fact]
        public void ABinomialCoefficientIsAFallingFactorialOverAFactorial()
            => Assert.Equal(MathS.Binomial(7, 3).InnerSimplified, (MathS.FallingFactorial(7, 3) / MathS.Factorial(3)).InnerSimplified);

        [Fact]
        public void ASymbolKeepsTheNode()
        {
            Assert.IsType<FallingFactorialf>("fallingfactorial(x, 3)".ToEntity().InnerSimplified);
            Assert.IsType<RisingFactorialf>("risingfactorial(2, n)".ToEntity().InnerSimplified);
        }

        /// <summary>
        /// Knuth's bars. A conjugate as the whole exponent of a power is bracketed, so that it does
        /// not read as a rising factorial.
        /// </summary>
        [Theory]
        [InlineData("fallingfactorial(x, n)", "fallingfactorial(x, n)", @"{x}^{\underline{n}}", "sympy.ff(x, n)")]
        [InlineData("factorialpower(x + 1, n)", "fallingfactorial(x + 1, n)", @"{\left(x+1\right)}^{\underline{n}}", "sympy.ff(x + 1, n)")]
        [InlineData("risingfactorial(x, n)", "risingfactorial(x, n)", @"{x}^{\overline{n}}", "sympy.rf(x, n)")]
        [InlineData("x ^ conj(n)", "x ^ conjugate(n)", @"{x}^{\left(\overline{n}\right)}", "x ** sympy.conjugate(n)")]
        public void TheyAreWrittenWithKnuthsBars(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
