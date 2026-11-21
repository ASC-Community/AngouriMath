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
    /// The exponential and logarithmic integrals took each part against 1, so a part below about 1e-50 was
    /// read as a residual of a cancellation and became 0: <c>Ei(-130)</c>, about <c>-2.66e-59</c>, and
    /// <c>li(10^(-60))</c>, about <c>-7.19e-63</c>, were 0, and <c>Ei(1 + 10^(-60) i)</c> lost its imaginary
    /// part. The series is a residual against its terms less the digits it is taken with beyond the working
    /// ones, and the asymptotic series against the products added to make each part. Found while looking for
    /// more values made like the modulus; no issue was filed.
    /// https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheExponentialIntegralKeepsASmallValueTest
    {
        [Theory]
        [InlineData("Ei(-130)", "-2.6574860270451376990240224925719467844442350722967E-59", "0")]
        [InlineData("Ei(1 + 10^(-60) * i)", "1.8951178163559367554665209343316342690170605817327", "2.7182818284590452353602874713526624977572470937E-60")]
        [InlineData("Ei(-300 + 10^(-60) * i)", "-1.7103842768045101157188773714693906847997891642764E-133", "3.1415926535897932384626433832795028841971693993751")]
        [InlineData("li(10^(-60))", "-7.186591671560689030143169180227800931177252676881E-63", "0")]
        [InlineData("li(2 + 10^(-60) * i)", "", "1.442695040888963407359924681001892137426645954153E-60")]
        // Controls: ordinary values.
        [InlineData("Ei(1)", "1.8951178163559367554665209343316342690170605817327", "0")]
        [InlineData("li(1/2)", "-0.37867104306108797672720718463656098055123404097821", "0")]
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
