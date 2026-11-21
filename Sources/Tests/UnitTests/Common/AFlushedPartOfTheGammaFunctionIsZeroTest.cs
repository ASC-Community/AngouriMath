//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The gamma function of a non-real number was made with the downcasting off, so a part below the
    /// context's exponents, which rounds to zero, was kept as a decimal zero with its exponent and sign:
    /// <c>gamma(316 * i)</c>, about <c>3.6e-217</c> in each part, printed as <c>0E-199 - -0E-199i</c>. It is made
    /// under the caller's downcasting at scale 0, the gamma function having no zero, so such a part is the
    /// number 0. Found while looking for more values made like the modulus; no issue was filed.
    /// https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AFlushedPartOfTheGammaFunctionIsZeroTest
    {
        [Theory]
        [InlineData("gamma(316 * i)", "0")]
        [InlineData("gamma(-150.5 + i)", "0")]
        [InlineData("(-120.5 + i)!", "-4E-199i")]
        public void TheFlushedPartIsTheNumberZero(string expression, string printed)
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            Assert.Equal(printed, value.Stringize());
            if (value.RealPart.EDecimal.IsZero)
                Assert.Equal(Number.Integer.Zero, value.RealPart);
        }
    }
}
