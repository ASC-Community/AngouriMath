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
    /// <c>arcosh</c> and <c>arsech</c> have their principal values, <c>ln(z + sqrt(z + 1) sqrt(z - 1))</c>
    /// and that of <c>1/z</c> (DLMF 4.37.19), where they were written <c>ln(z + sqrt(z^2 - 1))</c>: the one
    /// root takes the other sign wherever <c>Re z &lt; 0</c>, which negates the logarithm, so that
    /// <c>arcosh(-2)</c> was <c>-1.317 + pi i</c> and <c>arcosh(-1 - i)</c> was <c>-1.061 + 2.237 i</c>.
    /// The expected values are mpmath's, which on a cut takes the value from above.
    /// No issue was filed.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class InverseHyperbolicCosinePrincipalValueTest
    {
        private static bool Agree(Complex expected, Complex actual) => Complex.Abs(expected - actual) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        [InlineData("arcosh(-2)", 1.3169578969248167, 3.1415926535897932)]
        [InlineData("arcosh(-2i)", 1.4436354751788103, -1.5707963267948966)]
        [InlineData("arcosh(-1-i)", 1.0612750619050357, -2.2370357592874119)]
        [InlineData("arcosh(3)", 1.7627471740390861, 0.0)]
        [InlineData("arcosh(0.5)", 0.0, 1.0471975511965977)]
        [InlineData("arcosh(-0.5)", 0.0, 2.0943951023931955)]
        [InlineData("arcosh(2i)", 1.4436354751788103, 1.5707963267948966)]
        [InlineData("arcosh(-3+i)", 1.8241987021938827, 2.8038915443242407)]
        [InlineData("arsech(-2)", 0.0, 2.0943951023931955)]
        [InlineData("arsech(-2i)", 0.48121182505960345, 1.5707963267948966)]
        [InlineData("arsech(-1-i)", 0.53063753095251783, 2.0230747739460873)]
        [InlineData("arsech(3)", 0.0, 1.2309594173407747)]
        [InlineData("arsech(0.5)", 1.3169578969248167, 0.0)]
        [InlineData("arsech(-0.5)", 1.3169578969248167, 3.1415926535897932)]
        [InlineData("arsech(2i)", 0.48121182505960345, -1.5707963267948966)]
        [InlineData("arsech(-3+i)", 0.10458149883937041, -1.873777434142505)]
        public void ThePrincipalValue(string expression, double re, double im)
        {
            var value = ((Entity.Number.Complex)expression.ToEntity().Evaled).ToNumerics();
            Assert.True(Agree(new Complex(re, im), value), $"{expression}: {value}, where {new Complex(re, im)}");
        }

        // The derivative is the principal one's, 1/(sqrt(x - 1) sqrt(x + 1)): at -3 that is -1/(2 sqrt(2)),
        // where 1/sqrt(x^2 - 1) is +1/(2 sqrt(2)).
        [Fact]
        public void TheDerivativeAtANegativeArgument()
        {
            var derivative = "arcosh(x)".ToEntity().Differentiate("x").Substitute("x", -3).Evaled;
            var value = ((Entity.Number.Complex)derivative).ToNumerics();
            Assert.True(Agree(new Complex(-1 / (2 * System.Math.Sqrt(2)), 0), value), value.ToString());
        }

        // cosh undoes it everywhere, as it did before: the defect was the branch, not the inverse.
        [Theory]
        [InlineData("-2")]
        [InlineData("-1-i")]
        [InlineData("3+2i")]
        [InlineData("-0.5")]
        public void CoshUndoesIt(string z)
        {
            var back = ((Entity.Number.Complex)$"cosh(arcosh({z}))".ToEntity().Evaled).ToNumerics();
            var expected = ((Entity.Number.Complex)z.ToEntity().Evaled).ToNumerics();
            Assert.True(Agree(expected, back), $"{z}: {back}");
        }
    }
}
