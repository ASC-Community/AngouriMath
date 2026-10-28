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
    /// The complex conjugate, <c>conjugate(z)</c> or <c>conj(z)</c>, and the principal argument
    /// <c>arg(z)</c>. Chapter III of Modenov's <i>Problems in Geometry</i> writes every point as a
    /// complex number and every statement in <c>z</c> and its conjugate, and its angles are
    /// arguments; the parser refused all three names.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2145">#2145</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ConjugateAndArgumentTest
    {
        [Fact]
        public void BothNamesAreTheNode()
        {
            Assert.Equal(new Conjugatef("z"), "conjugate(z)".ToEntity());
            Assert.Equal(new Conjugatef("z"), "conj(z)".ToEntity());
            Assert.Equal(new Conjugatef("z"), MathS.Conjugate("z"));
        }

        [Theory]
        [InlineData("conj(3 + 4i)", "3 - 4i")]
        [InlineData("conj(i)", "-i")]
        [InlineData("conj(2)", "2")]
        [InlineData("conj(1/2)", "1/2")]
        [InlineData("conj(conj(z))", "z")]
        public void ANumberIsConjugatedAndTheConjugateIsAnInvolution(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>
        /// A field automorphism, so it is carried through the arithmetic and whole powers, and
        /// identities between conjugates are then decided as identities between symbols.
        /// </summary>
        [Theory]
        [InlineData("conj(a + b) - conj(a) - conj(b)")]
        [InlineData("conj(a - b) - conj(a) + conj(b)")]
        [InlineData("conj(a * b) - conj(a) * conj(b)")]
        [InlineData("conj(a / b) - conj(a) / conj(b)")]
        [InlineData("conj(a ^ 3) - conj(a) ^ 3")]
        [InlineData("conj(a * b + i) - (conj(a) * conj(b) - i)")]
        [InlineData("conj((z_2 - z_1) / (z_3 - z_1)) - (conj(z_2) - conj(z_1)) / (conj(z_3) - conj(z_1))")]
        public void TheConjugateIsCarriedThroughTheArithmetic(string identity)
            => Assert.Equal(Integer.Zero, Bare(identity.Simplify()));

        // A quotient's identity holds where its divisor is not zero, and says so.
        private static Entity Bare(Entity expr)
        {
            while (expr is Providedf(var inner, _))
                expr = inner;
            return expr;
        }

        /// <summary>
        /// Not through a root: <c>conjugate(sqrt(-1))</c> is <c>-i</c> and <c>sqrt(conjugate(-1))</c>
        /// is <c>i</c>, because the cut of the principal root lies on the negative real axis.
        /// </summary>
        [Fact]
        public void TheConjugateIsNotCarriedThroughARoot()
        {
            Assert.IsType<Conjugatef>("conj(sqrt(z))".ToEntity().InnerSimplified);
            Assert.Equal(-MathS.i, "conj(sqrt(-1))".ToEntity().Evaled);
        }

        /// <summary>
        /// What is known to be real is its own conjugate: a modulus, and a symbol declared real. A
        /// bare symbol is complex, and keeps its conjugate.
        /// </summary>
        [Fact]
        public void WhatIsKnownToBeRealIsItsOwnConjugate()
        {
            Assert.IsType<Conjugatef>("conj(x)".ToEntity().InnerSimplified);
            Assert.Equal("abs(z)".ToEntity(), "conj(abs(z))".ToEntity().InnerSimplified);
            Assert.Equal("domain(x, RR)".ToEntity(), "conj(domain(x, RR))".ToEntity().InnerSimplified);
        }

        /// <summary>
        /// The codomain setting is not read: what InnerSimplified gives is cached on the node, and
        /// the same node asked again under another setting would answer from the cache.
        /// </summary>
        [Fact]
        public void TheCodomainSettingDoesNotDecideIt()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.IsType<Conjugatef>("conj(w)".ToEntity().InnerSimplified);
        }

        [Fact]
        public void ItEvaluatesAtAComplexPoint()
            => Assert.Equal(Complex.Create(1, -2), "conj(z)".ToEntity().Substitute("z", Complex.Create(1, 2)).Evaled);

        [Fact]
        public void AVectorIsConjugatedComponentByComponent()
            => Assert.Equal("[1 - i, 2]".ToEntity().InnerSimplified, "conj([1 + i, 2])".ToEntity().InnerSimplified);

        /// <summary>
        /// Along the real line of the variable the step of the difference quotient is real, so the
        /// derivative of the conjugate is the conjugate of the derivative.
        /// </summary>
        [Fact]
        public void TheDerivativeIsTheConjugateOfTheDerivative()
            => Assert.Equal(Integer.Zero, ("derivative(conj(x^2 + i x), x)".ToEntity() - "2 conj(x) - i".ToEntity()).Simplify());

        [Fact]
        public void AnEquationInTheConjugateIsSolved()
            => Assert.Equal("{ 2 - i }".ToEntity(), "conj(x) = 2 + i".ToEntity().Solve("x").InnerSimplified);

        /// <summary>
        /// The principal argument of a number, exactly: the axes are 0, pi and +-pi/2, the left
        /// half-plane is turned by pi, and <c>arg(-1)</c> is <c>pi</c>, the end of
        /// <c>(-pi, pi]</c> that is included.
        /// </summary>
        [Theory]
        [InlineData("arg(1)", "0")]
        [InlineData("arg(5/2)", "0")]
        [InlineData("arg(-1)", "pi")]
        [InlineData("arg(i)", "pi/2")]
        [InlineData("arg(-3i)", "-pi/2")]
        [InlineData("arg(1 + i)", "pi/4")]
        [InlineData("arg(-1 + i)", "3/4 * pi")]
        [InlineData("arg(-1 - i)", "-3/4 * pi")]
        [InlineData("arg(1 - i)", "-pi/4")]
        [InlineData("arg(2 + i)", "arctan(1/2)")]
        [InlineData("arg(-2 + i)", "arctan(-1/2) + pi")]
        public void TheArgumentOfANumberIsExact(string written, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, written.ToEntity().InnerSimplified);

        /// <summary>Every angle fits <c>0 = 0 e^(i t)</c>.</summary>
        [Fact]
        public void TheArgumentOfZeroHasNoValue()
        {
            Assert.Equal(MathS.NaN, "arg(0)".ToEntity().InnerSimplified);
            Assert.Equal("not z = 0".ToEntity().InnerSimplified, "arg(z)".ToEntity().DomainCondition.InnerSimplified);
        }

        /// <summary>
        /// <c>z = |z| e^(i arg(z))</c>, evaluated at a point in each quadrant and on the negative
        /// real axis.
        /// </summary>
        [Theory]
        [InlineData(3, 4)]
        [InlineData(-3, 4)]
        [InlineData(-3, -4)]
        [InlineData(3, -4)]
        [InlineData(-2, 0)]
        public void TheModulusAndTheArgumentGiveBackTheNumber(int re, int im)
        {
            var z = Complex.Create(re, im);
            var back = "abs(z) * e ^ (i * arg(z)) - z".ToEntity().Substitute("z", z).EvalNumerical();
            Assert.True(Number.Abs(back).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-20")) < 0,
                $"|z| e^(i arg z) - z is {back} at {z}");
        }

        [Fact]
        public void TheArgumentIsRealAndStaysOnASymbol()
        {
            Assert.IsType<Argf>("arg(z)".ToEntity().InnerSimplified);
            Assert.Equal(Domain.Real, "arg(z)".ToEntity().Codomain);
        }

        [Theory]
        [InlineData("conj(z)", "conjugate(z)", @"\overline{z}", "sympy.conjugate(z)")]
        [InlineData("arg(z)", "arg(z)", @"\arg\left(z\right)", "sympy.arg(z)")]
        [InlineData("conj(a + b)", "conjugate(a + b)", @"\overline{a+b}", "sympy.conjugate(a + b)")]
        public void ItIsWrittenAsTheConjugate(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }
    }
}
