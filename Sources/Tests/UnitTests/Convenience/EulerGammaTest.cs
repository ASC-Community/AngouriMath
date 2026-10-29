//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// Euler's constant, <c>eulergamma</c>, and the digamma and harmonic values it makes exact. It was a
    /// variable, and <c>digamma(1)</c> stayed as written.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class EulerGammaTest
    {
        private static void Close(Entity value, string expected, string tolerance)
        {
            var real = Assert.IsAssignableFrom<Number.Real>(value.EvalNumerical());
            Assert.True(real.EDecimal.Subtract(EDecimal.FromString(expected)).Abs().CompareTo(EDecimal.FromString(tolerance)) < 0, real.Stringize());
        }

        /// <summary>Against mpmath, to the default hundred digits and to a hundred and fifty.</summary>
        [Fact]
        public void ItIsEulersConstant()
        {
            Close("eulergamma".ToEntity(), "0.5772156649015328606065120900824024310421593359399235988057672348848677267776646709369470632917467495", "1e-98");
            using var _ = MathS.Settings.DecimalPrecisionContext.Set(new EContext(150, ERounding.HalfEven, -100000, 100000, false));
            Close(MathS.eulergamma, "0.577215664901532860606512090082402431042159335939923598805767234884867726777664670936947063291746749514631447249807082480960504014486542836224173997644", "1e-148");
        }

        [Fact]
        public void ItIsAConstantSpelledOutOrAsMathematicaSpellsIt()
        {
            Assert.IsType<Constant>("eulergamma".ToEntity());
            Assert.Equal(MathS.eulergamma, "EulerGamma".ToEntity());
            Assert.Equal("eulergamma", "EulerGamma".ToEntity().Stringize());
            Assert.IsNotType<Constant>("γ".ToEntity());
            Assert.Equal(Number.Integer.One, "gamma(1)".ToEntity().Simplify());
        }

        [Fact]
        public void ItIsDrawnAsGammaAndExportedAsSymPysConstant()
        {
            Assert.Equal(@"\mathrm{\gamma}", MathS.eulergamma.Latexize());
            Assert.Equal("sympy.EulerGamma", MathS.eulergamma.ToSymPy());
            Assert.Equal(MathS.eulergamma, MathS.eulergamma.Stringize().ToEntity());
            var json = System.Text.Json.JsonSerializer.Serialize<Entity>(MathS.eulergamma);
            Assert.IsType<Constant>(System.Text.Json.JsonSerializer.Deserialize<Entity>(json));
        }

        /// <summary>A name an operator declares is the name, as <c>sum(e, e, 1, 3)</c> is <c>6</c>.</summary>
        [Fact]
        public void ADeclaredNameIsTheName()
            => Assert.Equal(Number.Integer.Create(6), "sum(eulergamma, eulergamma, 1, 3)".ToEntity().Simplify());

        [Fact]
        public void ItCompiles()
        {
            Assert.Equal(1.5772156649015329, "x + eulergamma".ToEntity().Compile("x").Call(1).Real, 15);
            Assert.Equal(1.5772156649015329, "x + eulergamma".ToEntity().Compile<double, double>("x")(1), 15);
        }

        /// <summary>
        /// Exact at a whole or half-whole argument, and equal there to mpmath's value, to 30 digits:
        /// <c>psi(1) = -gamma</c>, <c>psi(1/2) = -gamma - 2 ln 2</c>, and the recurrence from either.
        /// </summary>
        [Theory]
        [InlineData("digamma(1)", "-0.5772156649015328606065120900824024310422")]
        [InlineData("digamma(2)", "0.4227843350984671393934879099175975689578")]
        [InlineData("digamma(3)", "0.9227843350984671393934879099175975689578")]
        [InlineData("digamma(1/2)", "-1.963510026021423479440976332998755567193")]
        [InlineData("digamma(3/2)", "0.03648997397857652055902366700124443280684")]
        [InlineData("digamma(-1/2)", "0.03648997397857652055902366700124443280684")]
        [InlineData("digamma(-3/2)", "0.7031566406452431872256903336679110994735")]
        [InlineData("digamma(5/2)", "0.7031566406452431872256903336679110994735")]
        public void TheDigammaFunctionIsExactAtAWholeOrHalfWholeArgument(string written, string expected)
        {
            var exact = written.ToEntity().InnerSimplified;
            Assert.DoesNotContain(exact.Nodes, node => node is Polygammaf);
            Assert.Contains(MathS.eulergamma, exact.Nodes);
            Close(exact, expected, "1e-30");
        }

        [Fact]
        public void TheDigammaFunctionAtOneIsMinusEulersConstant()
        {
            Assert.Equal("-eulergamma", "digamma(1)".ToEntity().Simplify().Stringize());
            Assert.Equal(Number.Integer.Zero, ("digamma(1)".ToEntity() + MathS.eulergamma).Simplify());
        }

        /// <summary>Exact at a half-whole <c>n</c> for <c>m = 1</c> and an even <c>m</c>, and equal to mpmath's value.</summary>
        [Theory]
        [InlineData("harmonicnumber(1/2)", "0.613705638880109381165535757083646863849")]
        [InlineData("harmonicnumber(-1/2)", "-1.386294361119890618834464242916353136151")]
        [InlineData("harmonicnumber(3/2)", "1.280372305546776047832202423750313530516")]
        [InlineData("harmonicnumber(1/2, 2)", "0.7101318663035471270551696667079496215621")]
        [InlineData("harmonicnumber(3/2, 2)", "1.154576310747991571499614111152394066007")]
        public void TheHarmonicNumberIsExactAtAHalfWholeArgument(string written, string expected)
        {
            var exact = written.ToEntity().InnerSimplified;
            Assert.DoesNotContain(exact.Nodes, node => node is HarmonicNumberf or Polygammaf);
            Assert.DoesNotContain(MathS.eulergamma, exact.Nodes);
            Close(exact, expected, "1e-30");
        }

        /// <summary>An odd order from 3 wants <c>zeta(3)</c>, which has no closed form here.</summary>
        [Fact]
        public void AnOddOrderOfTheHarmonicNumberStaysAsWritten()
            => Assert.IsType<HarmonicNumberf>("harmonicnumber(1/2, 3)".ToEntity().InnerSimplified);
    }
}
