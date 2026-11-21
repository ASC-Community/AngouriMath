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
    /// The zeta function took each part against 1, so a part below about 1e-50 was read as a residual
    /// of a cancellation and became 0: <c>zeta(-2 + 10^(-60) i)</c>, about <c>-3.04e-62 i</c>, was 0, and
    /// every <c>zeta(s + 10^(-60) i)</c> lost its imaginary part. A part is now a residual against what the
    /// value is known to: the largest term of the sum, or the size of the reflected product, less the
    /// digits taken beyond the working ones. Found while looking for more values made like the modulus;
    /// no issue was filed. https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheZetaFunctionKeepsASmallPartTest
    {
        [Theory]
        [InlineData("zeta(-2 + 10^(-60) * i)", "3.288175809371259779497413310459545484454729158151906046148726E-122", "-3.044845705839327078025153047115477664700048354497393625297189E-62")]
        [InlineData("zeta(-3/2 + 10^(-60) * i)", "-0.02548520188983303594954298691070474546902498460097299683464549834924937718833927859709251894752436061", "-7.630925532055088662031899802550575518207158928525969563453814E-62")]
        [InlineData("zeta(1/2 + 10^(-60) * i)", "-1.460354508809586812889499152515298012467229331012581490542886087825530529474500625276419375463356820", "-3.922646139209151727471531446714599513730323971506505209568298E-60")]
        [InlineData("zeta(2 + 10^(-60) * i)", "1.644934066848226436472415166646025189218949901206798437735558229370007470403200873833628900619758705", "-9.375482543158437537025740945678649778978602886148299258854335E-61")]
        [InlineData("zeta(10^(-60) * i)", "-0.5", "-9.189385332046727417803297364056176398613974736377834128171515E-61")]
        // The first nontrivial zero to 100 digits is not the zero, and zeta there is known to all its digits.
        [InlineData("zeta(1/2 + 14.13472514173469379045725198356247027078425711569924317568556746014996342980925676494901039317156101 * i)", "3.46566137387746220435350583245693482164309905806837523591772E-100", "-2.17693999338466023179250337844518676470365168248218437158132E-99")]
        // Controls: a trivial zero is exactly 0, and real values have no imaginary part.
        [InlineData("zeta(-2)", "0", "0")]
        [InlineData("zeta(-7/2)", "0.004441011335479431958534658017819775086214245441800269408084359343818735511988967191747276231455519661", "0")]
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
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString("1E-40"))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
