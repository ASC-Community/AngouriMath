//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The harmonic numbers, <c>1 + 1/2^m + ... + 1/n^m</c> on the whole numbers and
    /// <c>ζ(m) - ζ(m, n + 1)</c> off them, as Mathematica's <c>HarmonicNumber</c>. The name was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class HarmonicNumberTest
    {
        [Theory]
        [InlineData("harmonicnumber(0)", "0")]
        [InlineData("harmonicnumber(1)", "1")]
        [InlineData("harmonicnumber(4)", "25/12")]
        [InlineData("harmonicnumber(4, 2)", "205/144")]
        [InlineData("harmonicnumber(3, 0)", "3")]
        [InlineData("harmonicnumber(3, -1)", "6")]
        [InlineData("harmonicnumber(2, 1/2)", "1 + 1/sqrt(2)")]
        public void ItIsTheSumOnTheWholeNumbers(string written, string expected)
            => Assert.Equal(Number.Integer.Zero, (written.ToEntity() - expected.ToEntity()).Simplify());

        /// <summary>A negative integer is a pole of ζ(m, n + 1), for an order that is not a whole number &lt;= 0.</summary>
        [Theory]
        [InlineData("harmonicnumber(-1)")]
        [InlineData("harmonicnumber(-3, 2)")]
        public void ItHasNoValueAtANegativeInteger(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().InnerSimplified);

        /// <summary>Against mpmath, to 30 digits: H(1/2) = 2 - 2 ln 2, H(-1/2) = -2 ln 2.</summary>
        [Theory]
        [InlineData("harmonicnumber(1/2)", "0.613705638880109381165535757084")]
        [InlineData("harmonicnumber(5/2)", "1.68037230554677604783220242375")]
        [InlineData("harmonicnumber(-1/2)", "-1.38629436111989061883446424292")]
        [InlineData("harmonicnumber(1/2, 2)", "0.710131866303547127055169666708")]
        public void OffTheWholeNumbersItIsThePolygammaForm(string written, string expected)
        {
            var value = Assert.IsAssignableFrom<Number.Real>(written.ToEntity().EvalNumerical());
            var difference = value.EDecimal.Subtract(EDecimal.FromString(expected)).Abs();
            Assert.True(difference.CompareTo(EDecimal.FromString("1e-25")) < 0, value.Stringize());
        }

        /// <summary>H(n) - H(n - 1) = 1/n, at n = 7/3 as well as at a whole n.</summary>
        [Fact]
        public void ConsecutiveHarmonicNumbersDifferByAReciprocal()
        {
            var at = (Entity)Number.Rational.Create(7, 3);
            var difference = (MathS.HarmonicNumber(at) - MathS.HarmonicNumber(at - 1)).EvalNumerical();
            var expected = (1 / at).EvalNumerical();
            Assert.True(((Number.Real)(difference - expected).EvalNumerical()).EDecimal.Abs().CompareTo(EDecimal.FromString("1e-25")) < 0, difference.Stringize());
        }

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<HarmonicNumberf>("harmonicnumber(n)".ToEntity().InnerSimplified);

        [Theory]
        [InlineData("harmonicnumber(n)", "harmonicnumber(n)", @"\operatorname{H}_{n}", "sympy.harmonic(n)")]
        [InlineData("harmonicnumber(n, 2)", "harmonicnumber(n, 2)", @"\operatorname{H}_{n}^{\left(2\right)}", "sympy.harmonic(n, 2)")]
        public void ItIsWrittenAsASubscriptedH(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
