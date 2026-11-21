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
    /// The sine, cosine, hyperbolic sine and hyperbolic cosine integrals took each part against 1, or the
    /// odd ones against their argument, so a part below about 1e-50 of that was read as a residual of a
    /// cancellation and became 0: the imaginary part of each at <c>1 + 10^(-60) i</c>, which is
    /// <c>10^(-60)</c> times the integrand at 1, was dropped. The series is a residual against its terms less
    /// the digits it is taken with beyond the working ones, and the exponential integral's form against
    /// half the scales of the two it adds. Found while looking for more values made like the modulus; no
    /// issue was filed. https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheTrigonometricIntegralsKeepASmallPartTest
    {
        [Theory]
        // Im f(1 + e i) is e f'(1) to within e^3: sin 1, cos 1, sinh 1 and cosh 1.
        [InlineData("Si(1 + 10^(-60) * i)", "0.946083070367183014941353313823179657812", "8.414709848078965066525023216302989996226E-61")]
        [InlineData("Ci(1 + 10^(-60) * i)", "0.337403922900968134662646203889150769997", "5.403023058681397174009366074429766037323E-61")]
        [InlineData("Shi(1 + 10^(-60) * i)", "1.057250875375728514571842354895877959024", "1.175201193643801456882381850595600815155E-60")]
        [InlineData("Chi(1 + 10^(-60) * i)", "0.837866940980208240894678579435756309993", "1.543080634815243778477905620757061682601E-60")]
        // Controls: an odd one at a small argument, and the negative real axis.
        [InlineData("Si(10^(-60))", "1E-60", "0")]
        [InlineData("Ci(-1)", "0.337403922900968134662646203889150769997", "3.141592653589793238462643383279502884197")]
        public void ThePartIsKept(string expression, string real, string imaginary)
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            AssertNear(real, value.RealPart.EDecimal);
            AssertNear(imaginary, value.ImaginaryPart.EDecimal);
        }

        private static void AssertNear(string expectedText, EDecimal actual)
        {
            var expected = EDecimal.FromString(expectedText);
            if (expected.IsZero)
                Assert.True(actual.IsZero, $"expected 0, got {actual}");
            else
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-38"))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
