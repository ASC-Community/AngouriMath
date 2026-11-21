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
    /// The gamma function of a real number was made as any decimal is, against 1, so a value below about
    /// 1e-50 was read as a residual of a cancellation and became 0: <c>gamma(-60.5)</c>, about
    /// <c>-4.84e-83</c>, was 0, while <c>gamma(-60.5 + i)</c>, made without the downcasting, kept its
    /// value. The gamma function has no zero, so its value is never such a residual. Found while looking
    /// for more values made like the modulus; no issue was filed.
    /// https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheGammaFunctionKeepsASmallValueTest
    {
        [Theory]
        [InlineData("gamma(-60.5)", "-4.8439543711382002700119940480168604753441571744854E-83")]
        [InlineData("(-61.5)!", "-4.8439543711382002700119940480168604753441571744854E-83")]
        [InlineData("gamma(-80.25)", "-2.0716627810095334216571624686096103222198146395678E-119")]
        [InlineData("gamma(-100.5)", "-3.3536908198076786422080996927145924061819485006665E-159")]
        // Controls: an ordinary value, and a whole one, which stays exact.
        [InlineData("gamma(1/2)", "1.7724538509055160272981674833411451827975494561224")]
        public void TheValueIsKept(string expression, string expectedText)
        {
            var value = Assert.IsAssignableFrom<Number.Real>(expression.ToEntity().Evaled);
            var expected = EDecimal.FromString(expectedText);
            Assert.True(value.EDecimal.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-40"))) < 0,
                $"expected {expected}, got {value.EDecimal}");
        }

        [Fact]
        public void AWholeValueStaysExact() => Assert.Equal(Number.Integer.Create(24), "gamma(5)".ToEntity().Evaled);
    }
}
