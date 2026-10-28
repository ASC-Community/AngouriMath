//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The Möbius function, the divisor function, the two counts of prime factors and the
    /// prime-counting function. Each name was read as a product, <c>mobius(6)</c> as
    /// <c>mobius * 6</c>. <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class NumberTheoryFunctionsTest
    {
        [Theory]
        [InlineData("mobius(1)", "1")]
        [InlineData("mobius(2)", "-1")]
        [InlineData("mobius(4)", "0")]
        [InlineData("mobius(6)", "1")]
        [InlineData("mobius(30)", "-1")]
        [InlineData("mobius(2 * 3 * 5 * 7)", "1")]
        [InlineData("moebiusmu(12)", "0")]
        [InlineData("primenu(1)", "0")]
        [InlineData("primenu(12)", "2")]
        [InlineData("primenu(30)", "3")]
        [InlineData("primeomega(1)", "0")]
        [InlineData("primeomega(12)", "3")]
        [InlineData("primeomega(1024)", "10")]
        [InlineData("divisorsigma(0, 12)", "6")]
        [InlineData("divisorsigma(1, 12)", "28")]
        [InlineData("divisorsigma(2, 6)", "50")]
        [InlineData("divisorsigma(1, 1)", "1")]
        [InlineData("divisorsigma(-1, 6)", "2")]
        [InlineData("divisorsigma(-2, 2)", "5/4")]
        [InlineData("primepi(1)", "0")]
        [InlineData("primepi(2)", "1")]
        [InlineData("primepi(10)", "4")]
        [InlineData("primepi(10.5)", "4")]
        [InlineData("primepi(100)", "25")]
        [InlineData("primepi(-3)", "0")]
        [InlineData("primepi(1000000)", "78498")]
        public void TheyAreComputedOnTheirDomains(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>Any order: the square roots of the divisors of 4 are 1, sqrt(2) and 2.</summary>
        [Fact]
        public void TheDivisorFunctionTakesAnyOrder()
            => Assert.Equal(Number.Integer.Zero, ("divisorsigma(1/2, 4) - (3 + sqrt(2))".ToEntity()).Simplify());

        /// <summary>
        /// The functions of a factorisation are defined on the positive integers, and the
        /// prime-counting function on the reals.
        /// </summary>
        [Theory]
        [InlineData("mobius(0)")]
        [InlineData("mobius(-6)")]
        [InlineData("mobius(1/2)")]
        [InlineData("primenu(0)")]
        [InlineData("primeomega(-4)")]
        [InlineData("divisorsigma(1, 0)")]
        [InlineData("divisorsigma(1, 3/2)")]
        [InlineData("primepi(i)")]
        public void TheyHaveNoValueOffTheirDomains(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().InnerSimplified);

        /// <summary>
        /// A number whose part left after trial division up to a million is too large to call
        /// prime is not factorised, and a count of primes past ten million is not sieved; both are
        /// left as written rather than answered slowly. 1000003 and 1000033 are primes.
        /// </summary>
        [Theory]
        [InlineData("mobius(1000003 * 1000033)")]
        [InlineData("primeomega(2305843009213693951)")]
        [InlineData("primepi(10000001)")]
        [InlineData("mobius(n)")]
        [InlineData("divisorsigma(k, 6)")]
        [InlineData("divisorsigma(2, n)")]
        public void TheyAreLeftAsWrittenWhereTheyAreNotComputed(string written)
            => Assert.IsAssignableFrom<Function>(written.ToEntity().InnerSimplified);

        /// <summary>A small prime factor is found past the machine word: 2^100 is not square-free.</summary>
        [Fact]
        public void ALargeNumberWithSmallFactorsIsFactorised()
        {
            Assert.Equal(Number.Integer.Zero, MathS.NumberTheory.Mobius(Number.Integer.Create(PeterO.Numbers.EInteger.One.ShiftLeft(100))).InnerSimplified);
            Assert.Equal(Number.Integer.Create(100), MathS.NumberTheory.PrimeOmega(Number.Integer.Create(PeterO.Numbers.EInteger.One.ShiftLeft(100))).InnerSimplified);
        }

        /// <summary>The sum of mu(d) over the divisors d of n is 1 at n = 1 and 0 above it.</summary>
        [Theory]
        [InlineData(1, 1)]
        [InlineData(12, 0)]
        [InlineData(30, 0)]
        [InlineData(97, 0)]
        public void TheMobiusFunctionSumsToZeroOverTheDivisors(int n, int sum)
        {
            Entity total = 0;
            for (var d = 1; d <= n; d++)
                if (n % d == 0)
                    total += MathS.NumberTheory.Mobius(d);
            Assert.Equal(Number.Integer.Create(sum), total.InnerSimplified);
        }

        [Theory]
        [InlineData("mobius(n)", "mobius(n)", @"\operatorname{\mu}\left(n\right)", "sympy.mobius(n)")]
        [InlineData("divisorsigma(k, n)", "divisorsigma(k, n)", @"\operatorname{\sigma}_{k}\left(n\right)", "sympy.divisor_sigma(n, k)")]
        [InlineData("primenu(n)", "primenu(n)", @"\operatorname{\omega}\left(n\right)", "sympy.primenu(n)")]
        [InlineData("primeomega(n)", "primeomega(n)", @"\operatorname{\Omega}\left(n\right)", "sympy.primeomega(n)")]
        [InlineData("primepi(x)", "primepi(x)", @"\operatorname{\pi}\left(x\right)", "sympy.primepi(x)")]
        [InlineData("moebiusmu(n + 1)", "mobius(n + 1)", @"\operatorname{\mu}\left(n+1\right)", "sympy.mobius(n + 1)")]
        public void TheyAreWrittenByName(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }

        [Fact]
        public void TheyAreIntegers()
        {
            Assert.Equal(AngouriMath.Core.Domain.Integer, MathS.NumberTheory.Mobius("n").Codomain);
            Assert.Equal(AngouriMath.Core.Domain.NonNegativeInteger, MathS.NumberTheory.PrimeNu("n").Codomain);
            Assert.Equal(AngouriMath.Core.Domain.NonNegativeInteger, MathS.NumberTheory.PrimeOmega("n").Codomain);
            Assert.Equal(AngouriMath.Core.Domain.NonNegativeInteger, MathS.NumberTheory.PrimePi("x").Codomain);
        }
    }
}
