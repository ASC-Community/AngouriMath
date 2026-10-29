//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The polygamma functions <c>psi^(n)(z)</c>, the digamma function among them, and the derivative of the
    /// factorial they give: <c>(x!)' = x! psi(x + 1)</c>, which declined while the library had no digamma function.
    /// References from mpmath at 130 digits.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/171">#171</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class PolygammaTest
    {
        private static void AssertClose(string reference, EDecimal actual)
        {
            var expected = EDecimal.FromString(reference);
            var error = actual.Subtract(expected, EContext.ForPrecision(200)).Abs().Divide(expected.Abs(), EContext.ForPrecision(10));
            Assert.True(error.LessThan(EDecimal.FromString("1E-55")), $"{actual} where it is {expected}");
        }

        [Theory]
        // -gamma, -gamma - 2 ln 2, by reflection, and the digamma function of a large argument.
        [InlineData("digamma(1)", "-0.577215664901532860606512090082402431042159335939923598805767")]
        [InlineData("digamma(1/2)", "-1.96351002602142347944097633299875556719315960466043410704713")]
        [InlineData("digamma(-1/2)", "0.0364899739785765205590236670012444328068403953395658929528727")]
        [InlineData("digamma(21/2)", "2.30300103429768637527259355084976605222629263212926398296056")]
        [InlineData("digamma(10^10)", "23.0258509298904568401790812135103087426776823862877297603333")]
        [InlineData("digamma(-10^6 + 1/2)", "13.8155105579643157707746153874811852456104531778043797538314")]
        // pi^2/6, and orders past one on either side of 0.
        [InlineData("polygamma(1, 1)", "1.64493406684822643647241516664602518921894990120679843773556")]
        [InlineData("polygamma(2, 3)", "-0.154113806319188570799476323022899981529972584680997763584543")]
        [InlineData("polygamma(3, -5/2)", "194.747876219187622421625517873890296434912770857870606876653")]
        [InlineData("polygamma(5, 1/3)", "87502.2152361966023795285638471692738850417457051333041487639")]
        public void IsItsValueOnTheRealLine(string expression, string reference)
            => AssertClose(reference, Assert.IsType<Real>(expression.EvalNumerical()).EDecimal);

        [Theory]
        [InlineData("digamma(1 + i)", "0.0946503206224769772718784827219107224762629717635416232329897", "1.07667404746858117413405079475000049044565626640381666557506")]
        [InlineData("polygamma(1, 1/4 + 2i)", "-0.0659875969316856927740067718219804136807514101395018917845139", "-0.502733085636287710672357390342666507577585357074072483065022")]
        [InlineData("digamma(-3/2 + i)", "0.809776810544048995668935476826367320162050302850132323665155", "2.67263282550818697494581681264808164030881630446709282234297")]
        public void IsItsValueOffTheRealLine(string expression, string real, string imaginary)
        {
            var value = Assert.IsType<Complex>(expression.EvalNumerical());
            AssertClose(real, value.RealPart.EDecimal);
            AssertClose(imaginary, value.ImaginaryPart.EDecimal);
        }

        [Theory]
        [InlineData("digamma(0)")]
        [InlineData("digamma(-3)")]
        [InlineData("polygamma(2, -1)")]
        public void IsUndefinedAtAPole(string expression)
            => Assert.True(expression.EvalNumerical() is Complex { IsNaN: true }, $"{expression} is {expression.EvalNumerical()}");

        [Fact]
        public void TheDerivativeOfTheFactorialIsWrittenWithTheDigammaFunction()
        {
            var derivative = "x!".ToEntity().Differentiate("x");
            Assert.Equal("x! * digamma(x + 1)".ToEntity().InnerSimplified, derivative.InnerSimplified);
            AssertClose("7.5367060105908028363609274595055854137470439843604584071654",
                Assert.IsType<Real>(derivative.Substitute("x", 3).EvalNumerical()).EDecimal);
        }

        [Fact]
        public void EachOrderIsTheDerivativeOfTheOneBefore()
        {
            Assert.Equal(MathS.Polygamma(1, "x"), "digamma(x)".ToEntity().Differentiate("x"));
            Assert.Equal((MathS.Polygamma(3, "x^2") * (2 * MathS.Var("x"))).InnerSimplified, "polygamma(2, x^2)".ToEntity().Differentiate("x").InnerSimplified);
        }

        /// <summary>
        /// An odd order at a whole or half-whole argument is a rational times <c>pi^(n + 1)</c> plus a rational
        /// (DLMF 5.15.2, 5.15.3); each is checked against its numerical value.
        /// </summary>
        [Theory]
        [InlineData("polygamma(1, 1)", "1/6 * pi ^ 2")]
        [InlineData("polygamma(1, 1/2)", "1/2 * pi ^ 2")]
        [InlineData("polygamma(3, 1)", "1/15 * pi ^ 4")]
        [InlineData("polygamma(1, 3)", "1/6 * pi ^ 2 - 5/4")]
        [InlineData("polygamma(1, -1/2)", "1/2 * pi ^ 2 + 4")]
        [InlineData("polygamma(3, 5/2)", "pi ^ 4 - 2624/27")]
        public void AnOddOrderAtAWholeOrHalfWholeArgumentIsExact(string expression, string exact)
        {
            var simplified = expression.ToEntity().InnerSimplified;
            Assert.Equal(exact.ToEntity().InnerSimplified, simplified);
            AssertClose(expression.EvalNumerical().Stringize(), Assert.IsType<Real>(simplified.EvalNumerical()).EDecimal);
        }

        /// <summary>An even order from 2 wants zeta at an odd number, and an argument neither whole nor half-whole
        /// has no closed form here: it stays as written.</summary>
        [Theory]
        [InlineData("digamma(1/3)")]
        [InlineData("polygamma(2, 1)")]
        [InlineData("polygamma(1, 1/3)")]
        public void OtherwiseItStaysAsWritten(string expression)
            => Assert.IsType<Entity.Polygammaf>(expression.ToEntity().InnerSimplified);

        /// <summary>Each order integrates to the one before, and the digamma function to <c>ln Gamma</c>.</summary>
        [Theory]
        [InlineData("polygamma(2, x)", "polygamma(1, x)")]
        [InlineData("polygamma(1, 3 * x + 2)", "digamma(3 * x + 2) / 3")]
        [InlineData("digamma(x)", "ln((x - 1)!)")]
        [InlineData("digamma(2 * x)", "ln((2 * x - 1)!) / 2")]
        public void IntegratesToTheOrderBefore(string integrand, string antiderivative)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0).Simplify();
            Assert.Equal(antiderivative.ToEntity().Simplify(), integral);
            var back = integral.Differentiate("x").Substitute("x", Rational.Create(7, 3)).EvalNumerical();
            var expected = integrand.ToEntity().Substitute("x", Rational.Create(7, 3)).EvalNumerical();
            AssertClose(expected.Stringize(), Assert.IsType<Real>(back).EDecimal);
        }

        [Theory]
        [InlineData("digamma(x)", "digamma(x)", @"\psi\left(x\right)", "sympy.digamma(x)")]
        [InlineData("polygamma(2, x)", "polygamma(2, x)", @"\psi^{(2)}\left(x\right)", "sympy.polygamma(2, x)")]
        [InlineData("polygamma(0, x)", "digamma(x)", @"\psi\left(x\right)", "sympy.digamma(x)")]
        public void IsParsedAndPrinted(string written, string text, string latex, string sympy)
        {
            var entity = written.ToEntity();
            Assert.IsType<Entity.Polygammaf>(entity);
            Assert.Equal(text, entity.Stringize());
            Assert.Equal(latex, entity.Latexize());
            Assert.Contains(sympy, MathS.ToSympyCode(entity));
            Assert.Equal(entity, entity.Stringize().ToEntity());
        }
    }
}
