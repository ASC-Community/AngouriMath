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
    /// The cotangent was <c>1/tan z</c>, made 0 wherever both parts were below <c>1e-16</c>, so
    /// <c>cot(pi/2 + 10^(-20))</c>, about <c>-10^(-20)</c>, was 0; and off the real line past the context's
    /// exponents <c>cot(1 - 3000 i)</c>, which is about <c>i</c>, was <c>NaN</c>. It is <c>cos x/sin x</c> on the
    /// real line, a residual of <c>cos x</c> as everywhere, and a form that neither cancels nor overflows off it.
    /// Found while looking for more absolute tolerances; no issue was filed.
    /// https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheCotangentKeepsASmallValueTest
    {
        [Theory]
        [InlineData("cot(pi/2 + 10^(-20))", "-1E-20", "0")]
        [InlineData("cot(3 * pi/2 - 10^(-30))", "1E-30", "0")]
        [InlineData("cot(pi/2 + 10^(-20) * i)", "", "-1E-20")]
        [InlineData("cot(1 - 3000 * i)", "", "1")]
        // Controls: an ordinary value, a zero that is only the rounding of pi, and the far upper half-plane.
        [InlineData("cot(1 + i)", "0.21762156185440268136513424360523807352075436916785", "-0.86801414289592494863584920891627388827343874994609")]
        [InlineData("cot(pi/2)", "0", "0")]
        [InlineData("cot(2 + 1000 * i)", "", "-1")]
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
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-38"))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
