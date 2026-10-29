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
    /// The Catalan numbers, <c>binomial(2n, n) / (n + 1)</c> on the whole numbers and
    /// <c>4^n Γ(n + 1/2) / (√π Γ(n + 2))</c> off them, as SymPy's <c>catalan</c>. The name was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class CatalanNumberTest
    {
        /// <summary>The values below 0 are SymPy's: <c>catalan(-1)</c> is <c>-1/2</c> and the rest are 0.</summary>
        [Theory]
        [InlineData("catalannumber(0)", "1")]
        [InlineData("catalannumber(1)", "1")]
        [InlineData("catalannumber(2)", "2")]
        [InlineData("catalannumber(3)", "5")]
        [InlineData("catalannumber(4)", "14")]
        [InlineData("catalannumber(10)", "16796")]
        [InlineData("catalannumber(-1)", "-1/2")]
        [InlineData("catalannumber(-2)", "0")]
        [InlineData("catalannumber(-3)", "0")]
        public void ItIsComputedAtTheIntegers(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>A negative half-integer is a pole of Γ(n + 1/2).</summary>
        [Theory]
        [InlineData("catalannumber(-1/2)")]
        [InlineData("catalannumber(-3/2)")]
        public void ItHasNoValueAtAPole(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().InnerSimplified);

        /// <summary>C(1/2) = 8/(3 pi), SymPy's 0.848826363156775124100713404653.</summary>
        [Fact]
        public void OffTheIntegersItIsTheGammaForm()
        {
            var value = Assert.IsAssignableFrom<Number.Real>("catalannumber(1/2)".ToEntity().EvalNumerical());
            var difference = value.EDecimal.Subtract(EDecimal.FromString("0.848826363156775124100713404653")).Abs();
            Assert.True(difference.CompareTo(EDecimal.FromString("1e-25")) < 0, value.Stringize());
        }

        /// <summary>C(n + 1) is the sum of C(i) C(n - i), the bracketings split at their outer product.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(7)]
        public void TheRecurrenceHolds(int n)
        {
            Entity sum = 0;
            for (var i = 0; i <= n; i++)
                sum += MathS.CatalanNumber(i) * MathS.CatalanNumber(n - i);
            Assert.Equal(MathS.CatalanNumber(n + 1).InnerSimplified, sum.InnerSimplified);
        }

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<CatalanNumberf>("catalannumber(n)".ToEntity().InnerSimplified);

        [Fact]
        public void ItIsWrittenAsASubscriptedC()
        {
            var parsed = "catalannumber(n + 1)".ToEntity();
            Assert.Equal("catalannumber(n + 1)", parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(@"\operatorname{C}_{n+1}", parsed.Latexize());
            Assert.Equal("sympy.catalan(n + 1)", parsed.ToSymPy());
        }
    }
}
