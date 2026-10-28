//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The real part <c>Re(z)</c> and the imaginary part <c>Im(z)</c>, written as Mathematica, Maple
    /// and ISO 80000-2 write them, and also as SymPy's <c>re</c> and <c>im</c> and MATLAB's
    /// <c>real</c> and <c>imag</c>. The lower-case names were read as products, <c>re(z)</c> being
    /// <c>re * z</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2145">#2145</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class RealAndImaginaryPartTest
    {
        [Fact]
        public void BothNamesOfEachAreTheNode()
        {
            Assert.Equal(new Ref("z"), "Re(z)".ToEntity());
            Assert.Equal(new Ref("z"), "re(z)".ToEntity());
            Assert.Equal(new Ref("z"), "real(z)".ToEntity());
            Assert.Equal(new Imf("z"), "Im(z)".ToEntity());
            Assert.Equal(new Imf("z"), "im(z)".ToEntity());
            Assert.Equal(new Imf("z"), "imag(z)".ToEntity());
        }

        /// <summary>The imaginary part is a real number: <c>im(3 + 4i)</c> is <c>4</c>, not <c>4i</c>.</summary>
        [Theory]
        [InlineData("re(3 + 4i)", "3")]
        [InlineData("im(3 + 4i)", "4")]
        [InlineData("re(-i)", "0")]
        [InlineData("im(-i)", "-1")]
        [InlineData("re(5/2)", "5/2")]
        [InlineData("im(5/2)", "0")]
        public void ANumberIsTakenApart(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>
        /// Both are real-linear: carried through sums, differences, and products and quotients by a
        /// real number. The conjugate keeps the real part and turns the imaginary one.
        /// </summary>
        [Theory]
        [InlineData("re(a + b) - re(a) - re(b)")]
        [InlineData("im(a - b) - im(a) + im(b)")]
        [InlineData("re(3 z) - 3 re(z)")]
        [InlineData("im(z / 2) - im(z) / 2")]
        [InlineData("re(conj(z)) - re(z)")]
        [InlineData("im(conj(z)) + im(z)")]
        public void TheyAreRealLinear(string identity)
            => Assert.Equal(Integer.Zero, identity.Simplify());

        /// <summary>
        /// Not through a product of two symbols, whose real part is <c>re(a) re(b) - im(a) im(b)</c>
        /// and not <c>re(a) re(b)</c>, and not through <c>i</c>, which turns one part into the other.
        /// </summary>
        [Theory]
        [InlineData("re(a * b)")]
        [InlineData("im(i * z)")]
        public void TheyAreNotCarriedThroughAComplexFactor(string written)
            => Assert.True(written.ToEntity().InnerSimplified is Ref or Imf);

        /// <summary>
        /// What is known to be real is its own real part, and has a value exactly where that has
        /// one. Its imaginary part is not taken as 0: <c>im(prime(x))</c> has no value at
        /// <c>x = -3</c>, where 0 would have one.
        /// </summary>
        [Fact]
        public void WhatIsKnownToBeRealIsItsOwnRealPart()
        {
            Assert.Equal("abs(z)".ToEntity(), "re(abs(z))".ToEntity().InnerSimplified);
            Assert.Equal("domain(x, RR)".ToEntity(), "re(domain(x, RR))".ToEntity().InnerSimplified);
            Assert.IsType<Imf>("im(prime(x))".ToEntity().Simplify());
            Assert.IsType<Ref>("re(z)".ToEntity().InnerSimplified);
        }

        /// <summary><c>z = re(z) + i im(z)</c>, evaluated at a point in each quadrant.</summary>
        [Theory]
        [InlineData(3, 4)]
        [InlineData(-3, 4)]
        [InlineData(-3, -4)]
        [InlineData(3, -4)]
        public void ThePartsGiveBackTheNumber(int re, int im)
        {
            var z = Complex.Create(re, im);
            var back = "re(z) + i * im(z) - z".ToEntity().Substitute("z", z).EvalNumerical();
            Assert.Equal(Integer.Zero, back);
        }

        [Fact]
        public void BothAreReal()
        {
            Assert.Equal(Domain.Real, "re(z)".ToEntity().Codomain);
            Assert.Equal(Domain.Real, "im(z)".ToEntity().Codomain);
        }

        /// <summary>Along the real line of the variable both commute with the derivative.</summary>
        [Fact]
        public void TheDerivativeIsThePartOfTheDerivative()
        {
            Assert.Equal(Integer.Zero, ("derivative(re(x^2 + i x), x)".ToEntity() - "2 re(x)".ToEntity()).Simplify());
            Assert.Equal(Integer.Zero, ("derivative(im(x^2 + i x), x)".ToEntity() - "2 im(x) + 1".ToEntity()).Simplify());
        }

        [Theory]
        [InlineData("re(z)", "Re(z)", @"\operatorname{Re}\left(z\right)", "sympy.re(z)")]
        [InlineData("Im(z)", "Im(z)", @"\operatorname{Im}\left(z\right)", "sympy.im(z)")]
        public void TheyAreWrittenByName(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
