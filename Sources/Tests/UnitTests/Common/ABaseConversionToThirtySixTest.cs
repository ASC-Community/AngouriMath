//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>FromBaseN</c> added its digits up in an <c>int</c>, so a number past 2^31 wrapped, and it read a digit that
    /// is not in the base: <c>"102"</c> in base 2 was 6. The digit table of both directions stopped at F, where the
    /// catalogue offers bases to 36. And <c>ToBaseN</c> wrote a rational through its decimal, rounded and the rounding
    /// multiplied up, so <c>1/7</c> in base 7 ran to 119 digits.
    /// https://github.com/asc-community/AngouriMath/issues/2343,
    /// https://github.com/asc-community/AngouriMath/issues/2344
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ABaseConversionToThirtySixTest
    {
        [Theory]
        [InlineData("80000000", 16, "2147483648")]
        [InlineData("100000000", 16, "4294967296")]
        [InlineData("2147483648", 10, "2147483648")]
        [InlineData("10000000000", 10, "10000000000")]
        [InlineData("-10000000000", 10, "-10000000000")]
        [InlineData("ff", 16, "255")]
        [InlineData("FF", 16, "255")]
        [InlineData("G", 17, "16")]
        [InlineData("z", 36, "35")]
        [InlineData("0.1", 3, "0.3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333")]
        public void IsRead(string digits, int numberBase, string expected)
            => Assert.Equal(EDecimal.FromString(expected), MathS.FromBaseN(digits, numberBase).EDecimal);

        [Theory]
        [InlineData("102", 2)]
        [InlineData("19", 8)]
        [InlineData("1g", 16)]
        [InlineData("1-2", 10)]
        [InlineData("12", 1)]
        [InlineData("12", 37)]
        public void ADigitNotInTheBaseIsRefused(string digits, int numberBase)
            => Assert.Throws<InvalidNumericSystemException>(() => MathS.FromBaseN(digits, numberBase));

        [Theory]
        [InlineData(35, 36, "Z")]
        [InlineData(35, 17, "21")]
        [InlineData(1099511627776, 16, "10000000000")]
        public void AWholeNumberIsWritten(long value, int numberBase, string expected)
            => Assert.Equal(expected, MathS.ToBaseN(Integer.Create(value), numberBase));

        [Theory]
        [InlineData(1, 7, 7, "0.1")]
        [InlineData(1, 3, 3, "0.1")]
        [InlineData(-5, 9, 3, "-0.12")]
        [InlineData(7, 4, 2, "1.11")]
        public void ARationalIsWrittenExactly(int numerator, int denominator, int numberBase, string expected)
            => Assert.Equal(expected, MathS.ToBaseN(Rational.Create(numerator, denominator), numberBase));

        [Fact]
        public void ARepeatingExpansionIsCutAtThePrecision()
        {
            var third = MathS.ToBaseN(Rational.Create(1, 3), 10);
            Assert.StartsWith("0.3333", third);
            Assert.Equal(new string('3', third.Length - 2), third.Substring(2));
            Assert.InRange(third.Length, 50, 200);
        }

        [Fact]
        public void ALargeNumberRoundTrips()
        {
            var large = EInteger.FromString("123456789012345678901234567890");
            Assert.Equal(EDecimal.FromEInteger(large), MathS.FromBaseN(MathS.ToBaseN(Integer.Create(large), 36), 36).EDecimal);
        }
    }
}
