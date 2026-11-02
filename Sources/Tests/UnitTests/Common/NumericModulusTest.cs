//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The <c>%</c> operator over the numeric types. The three of them used to answer three
    /// different ways -- <see cref="Integer"/> threw outright on a negative divisor,
    /// <see cref="Real"/> truncated, and <see cref="Rational"/> was wrong under every
    /// convention for a negative divisor -- so which answer you got depended on the static type
    /// at the call site rather than on the values.
    /// See https://github.com/asc-community/AngouriMath/issues/708.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class NumericModulusTest
    {
        /// <summary>
        /// Floored: the remainder takes the sign of the divisor. This is what SymPy,
        /// Mathematica and Maxima answer, and the convention under which the residues modulo n
        /// are the numbers from 0 to n - 1.
        /// </summary>
        [Theory]
        [InlineData(7, 3, 1)]
        [InlineData(-7, 3, 2)]
        [InlineData(7, -3, -2)]
        [InlineData(-7, -3, -1)]
        [InlineData(6, 3, 0)]
        [InlineData(-6, 3, 0)]
        [InlineData(6, -3, 0)]
        [InlineData(2, 5, 2)]
        [InlineData(-2, 5, 3)]
        public void TheThreeTypesAgree(int dividend, int divisor, int expected)
        {
            var a = Integer.Create(dividend);
            var b = Integer.Create(divisor);
            Assert.Equal(Integer.Create(expected), a % b);
            Assert.Equal((Real)Integer.Create(expected), (Real)a % (Real)b);
        }

        /// <summary>
        /// A negative divisor used to raise <c>ArithmeticException: Divisor is negative</c> from
        /// the arbitrary-precision layer, on an operator that is public and on ordinary input.
        /// </summary>
        [Fact]
        public void ANegativeDivisorDoesNotThrow() =>
            Assert.Equal(Integer.Create(-2), Integer.Create(7) % Integer.Create(-3));

        /// <summary>
        /// The rational cases, against SymPy's answers for the same four sign pairs. The last
        /// used to come back as -7/2 -- larger in magnitude than the divisor, so a remainder
        /// under no convention at all.
        /// </summary>
        [Theory]
        [InlineData(7, 2, 3, 1, 1, 2)]
        [InlineData(-7, 2, 3, 1, 5, 2)]
        [InlineData(7, 2, -3, 1, -5, 2)]
        [InlineData(-7, 2, -3, 1, -1, 2)]
        public void RationalsAgreeWithTheSameConvention(
            int aNum, int aDen, int bNum, int bDen, int expectedNum, int expectedDen) =>
            Assert.Equal(
                Rational.Create(expectedNum, expectedDen),
                (Rational)Rational.Create(aNum, aDen) % (Rational)Rational.Create(bNum, bDen));

        /// <summary>
        /// Two ratios are divided as ratios however they are typed, in the operator and in
        /// evaluation. Taken as decimals, 1/27 is rounded up in its last digit, the quotient of 6
        /// by it falls just short of 162, and 6 % (1/27), which is 0, came back as 1/27. A
        /// quotient wider than the working precision has no decimal remainder, and 10^200 % 7
        /// was NaN. Found by a check of compiled expressions against evaluation; no issue was
        /// filed.
        /// </summary>
        [Theory]
        [InlineData("6 mod (1/27)", "0")]
        [InlineData("(10^200) mod 7", "2")]
        [InlineData("(10^200 + 3) mod (-10)", "-7")]
        [InlineData("(10^100 + 1/2) mod 1", "1/2")]
        [InlineData("(5/2) mod (1/6)", "0")]
        [InlineData("(-5/2) mod (1/6)", "0")]
        [InlineData("(7/3) mod (2/9)", "1/9")]
        [InlineData("(-7/3) mod (2/9)", "1/9")]
        [InlineData("(7/3) mod (-2/9)", "-1/9")]
        public void ARemainderOfTwoRatiosIsExact(string expression, string expected)
        {
            Assert.Equal(expected.ToEntity(), expression.ToEntity().EvalNumerical());
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());
        }

        [Fact]
        public void ARemainderOfTwoRatiosTypedAsRealsIsExact() =>
            Assert.Equal(Integer.Zero, (Real)Integer.Create(6) % (Real)Rational.Create(1, 27));

        /// <summary>
        /// The remainder is always strictly smaller in magnitude than the divisor. That is what
        /// the Rational case was failing, and it holds whatever the signs.
        /// </summary>
        [Theory]
        [InlineData(7, 3)]
        [InlineData(-7, 3)]
        [InlineData(7, -3)]
        [InlineData(-7, -3)]
        [InlineData(100, 7)]
        [InlineData(-100, 7)]
        [InlineData(1, 1000)]
        [InlineData(-1, 1000)]
        public void TheRemainderIsSmallerThanTheDivisor(int dividend, int divisor)
        {
            var remainder = Integer.Create(dividend) % Integer.Create(divisor);
            Assert.True(remainder.Abs() < Integer.Create(divisor).Abs(),
                $"{dividend} % {divisor} came out as {remainder}");
        }

        /// <summary>
        /// And a % b is congruent to a modulo b, that is, a - (a % b) is a whole multiple of b.
        /// Between them these two properties are the definition.
        /// </summary>
        [Theory]
        [InlineData(7, 3)]
        [InlineData(-7, 3)]
        [InlineData(7, -3)]
        [InlineData(-7, -3)]
        [InlineData(-100, 7)]
        public void TheDifferenceIsAWholeMultipleOfTheDivisor(int dividend, int divisor)
        {
            var a = Integer.Create(dividend);
            var b = Integer.Create(divisor);
            Assert.Equal(Integer.Create(0), (a - a % b) % b);
        }
    }
}
