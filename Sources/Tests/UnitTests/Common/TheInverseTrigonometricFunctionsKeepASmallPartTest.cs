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
    /// The arcsine, arccosine and arctangent of a non-real number cancelled digits inside their formulas
    /// at the working precision and then took each part against 1: <c>arcsin(10^(-60) (1 + i))</c> was 0,
    /// and the imaginary parts of <c>arctan(1 + 10^(-60) i)</c> and <c>arcsin(1/2 + 10^(-60) i)</c> were
    /// dropped. And on the real line <c>arcsin</c> answered <c>pi/2</c> within <c>1e-16</c> of 1 and
    /// <c>arccos</c> was <c>pi/2 - arcsin</c>, so <c>arccos(1 - 10^(-20))</c>, 1.4e-10, was 0. Found while
    /// looking for more values made like the modulus; no issue was filed.
    /// https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheInverseTrigonometricFunctionsKeepASmallPartTest
    {
        [Theory]
        [InlineData("arctan(10^(-60) * (1 + i))", "1E-60", "1E-60")]
        [InlineData("arctan(1 + 10^(-60) * i)", "0.78539816339744830961566084581987572104929234984378", "5E-61")]
        [InlineData("arcsin(10^(-60) * (1 + i))", "1E-60", "1E-60")]
        [InlineData("arcsin(1/2 + 10^(-60) * i)", "0.5235987755982988730771072305465838140328615665625176368291574320513", "1.1547005383792515290182975610039149112952035025402537520372E-60")]
        [InlineData("arccos(1/2 + 10^(-60) * i)", "1.047197551196597746154214461093167628065723133125035273658314864102605", "-1.1547005383792515290182975610039149112952035025402537520372E-60")]
        // At the branch point 1 it is pi/2 - (1 - i) 10^(-30), to within 10^(-90).
        [InlineData("arcsin(1 + 10^(-60) * i)", "1.5707963267948966192313216916387514420985846996875529104874722961539", "1E-30")]
        [InlineData("arccos(1 - 10^(-20))", "1.4142135623730950488028672355116756577770092676309E-10", "0")]
        [InlineData("pi/2 - arcsin(1 - 10^(-20))", "1.4142135623730950488028672355116756577770092676309E-10", "0")]
        // Controls: ordinary values.
        [InlineData("arctan(2 + i)", "1.1780972450961724644234912687298135815739385247657", "0.17328679513998632735430803036454414201887503359006")]
        [InlineData("arccos(0.7)", "0.79539883018414355549096833892476432854279596104639", "0")]
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
