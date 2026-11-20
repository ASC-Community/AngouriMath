//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The complementary error function, the imaginary error function and the Lambert W function made
    /// their value as a residual of a cancellation of terms near 1, which takes a value below half the
    /// working digits for 0: <c>erfc(12)</c>, about 1.36e-64, was 0, and so was <c>lambertw(10^(-60) i)</c>.
    /// erfc is taken with the digits its cancellation loses, and W is found to its own size. Found while
    /// looking for more values made like the modulus; no issue was filed.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ASpecialFunctionKeepsASmallValueTest
    {
        [Theory]
        [InlineData("erfc(12)", "1.356261169205904212780306156590417572666782233288104929912379537485356904852124632255934580073936306E-64", "0")]
        [InlineData("erfc(12 + i)", "1.827671875310519132944360366491176870645696283478397620793574738030499920454210397913899211224554967E-64", "")]
        [InlineData("erfi(10^(-60) * i)", "0", "1.128379167095512573896158903121545171688101258657997713688171443421284936882986828973487320404214727E-60")]
        [InlineData("lambertw(10^(-60) * i)", "", "1E-60")]
        // Controls: ordinary values, and the left half-plane, where erfc is near 2.
        [InlineData("erfc(1)", "0.1572992070502851306587793649173907407039330020336970915400621021652827459039891587380166746518551115", "0")]
        [InlineData("erfc(-3)", "1.999977909503001414558627223870417679620152292912600750342761045157057543316379867732183745349184684", "0")]
        [InlineData("lambertw(1)", "0.5671432904097838729999686622103555497538157871865125081351310792230457930866845666932194469617522946", "0")]
        public void TheValueIsKept(string expression, string real, string imaginary)
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            AssertNear(real, value.RealPart.EDecimal);
            AssertNear(imaginary, value.ImaginaryPart.EDecimal);
        }

        // An empty string is a part not asserted.
        private static void AssertNear(string expectedText, EDecimal actual)
        {
            if (expectedText.Length == 0)
                return;
            var expected = EDecimal.FromString(expectedText);
            if (expected.IsZero)
                Assert.True(actual.IsZero, $"expected 0, got {actual}");
            else
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-40"))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
