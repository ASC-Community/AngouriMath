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
    /// The sine, cosine, tangent and logarithm of a non-real number made each part as a residual of a
    /// cancellation of terms near 1, which takes a value below half the working digits for 0, so
    /// <c>sin(pi + 10^(-60) i)</c> was 0. Only sin(re) and cos(re) cancel, as on the real line, and the
    /// argument of a logarithm cancels nothing. Found while looking for more values made that way after
    /// the modulus; no issue was filed.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AComplexFunctionKeepsASmallPartTest
    {
        [Theory]
        [InlineData("sin(pi + 10^(-60) * i)", "0", "-1E-60")]
        [InlineData("cos(pi / 2 + 10^(-60) * i)", "0", "-1E-60")]
        [InlineData("sin(10^(-60) * (1 + i))", "1E-60", "1E-60")]
        [InlineData("sinh(10^(-60) * i) / i", "1E-60", "0")]
        [InlineData("cos(10^(-60) * (1 + i))", "1", "-1E-120")]
        [InlineData("tan(10^(-60) * (1 + i))", "1E-60", "1E-60")]
        [InlineData("ln(1 + 10^(-60) * i)", "0", "1E-60")]
        [InlineData("sin(i * 10^(-150))", "0", "1E-150")]
        [InlineData("exp(10^(-60) * i)", "1", "1E-60")]
        [InlineData("exp(pi * i)", "-1", "0")]
        // Controls: an ordinary value, and a real part that is a residual and stays 0.
        [InlineData("sin(3 + 4i)", "3.85373803791937732161752894046373066706827494698903", "-27.0168132580039344880975437549921522633638656897")]
        [InlineData("tan(pi + 10i)", "0", "0.99999999587769276361959283713827574105081461849501")]
        public void EachPartIsItsValue(string expression, string real, string imaginary)
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            AssertNear(EDecimal.FromString(real), value.RealPart.EDecimal);
            AssertNear(EDecimal.FromString(imaginary), value.ImaginaryPart.EDecimal);
        }

        private static void AssertNear(EDecimal expected, EDecimal actual)
        {
            if (expected.IsZero)
                Assert.True(actual.IsZero, $"expected 0, got {actual}");
            else
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-40"))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
