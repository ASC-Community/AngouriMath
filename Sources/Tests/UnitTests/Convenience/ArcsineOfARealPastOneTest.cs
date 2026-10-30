//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>arcsin</c> and <c>arccos</c> of a real number past 1 have their value, <c>sign(x) pi/2 - i arccosh|x|</c> and
    /// <c>pi/2 - arcsin(x)</c>, where for one argument in ten they were NaN: the rounded <c>(|w + 1| - |w - 1|) / 2</c>
    /// came out a little past 1, whose decimal arcsine is NaN. <c>arcsin(6 + pi)</c> was one.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2244">#2244</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ArcsineOfARealPastOneTest
    {
        private static double Arccosh(double x) => System.Math.Log(x + System.Math.Sqrt(x * x - 1));

        private static bool Agree(Complex expected, Complex actual) => Complex.Abs(expected - actual) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        [InlineData("pi")]
        [InlineData("e")]
        [InlineData("sqrt(2)")]
        [InlineData("sqrt(3)")]
        [InlineData("ln(3)")]
        public void BothHaveTheirValue(string constant)
        {
            for (var k = 1; k <= 40; k++)
                foreach (var sign in new[] { "", "-" })
                {
                    var argument = $"{sign}({k} + {constant})";
                    var x = ((Entity.Number.Complex)argument.ToEntity().Evaled).ToNumerics().Real;
                    if (System.Math.Abs(x) <= 1)
                        continue;
                    var arcsin = ((Entity.Number.Complex)$"arcsin({argument})".ToEntity().Evaled).ToNumerics();
                    var arccos = ((Entity.Number.Complex)$"arccos({argument})".ToEntity().Evaled).ToNumerics();
                    var expectedArcsin = new Complex(System.Math.Sign(x) * System.Math.PI / 2, -Arccosh(System.Math.Abs(x)));
                    var expectedArccos = new Complex(x > 0 ? 0 : System.Math.PI, Arccosh(System.Math.Abs(x)));
                    Assert.True(Agree(expectedArcsin, arcsin), $"arcsin({argument}): {arcsin}, where {expectedArcsin}");
                    Assert.True(Agree(expectedArccos, arccos), $"arccos({argument}): {arccos}, where {expectedArccos}");
                }
        }
    }
}
