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

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Compiled arithmetic of numbers that are not real is evaluation's at a whole power and at an infinity. A whole power
    /// is a product of its factors, so <c>i^2</c> is <c>-1</c> where <see cref="Complex.Pow(Complex, Complex)"/> made it
    /// <c>(-1, 1.2e-16)</c>; a part that is 0 times an infinite part is 0, so <c>i * x</c> at <c>+oo</c> is <c>+oo i</c> and
    /// not <c>(NaN, ∞)</c>; a quotient by a real number is taken part by part; and <c>sgn(x)</c> at an infinity is its sign.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2247">#2247</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ComplexArithmeticCompilationTest
    {
        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        private static Complex Evaluated(string expression, string at)
            => ((Entity.Number.Complex)expression.ToEntity().Substitute("x", at.ToEntity()).Evaled).ToNumerics();

        private static bool HasNaN(Complex z) => double.IsNaN(z.Real) || double.IsNaN(z.Imaginary);

        private static void BothCompilersGive(string expression, string at, System.Func<Complex, Complex, bool> agree)
        {
            var expected = Evaluated(expression, at);
            var fast = expression.ToEntity().Compile("x").Call(Numeric(at));
            var linq = expression.ToEntity().Compile<Complex, Complex>("x")(Numeric(at));
            Assert.True(agree(expected, fast), $"{expression} at {at}: {expected} evaluated, {fast} by FastExpression");
            Assert.True(agree(expected, linq), $"{expression} at {at}: {expected} evaluated, {linq} by LINQ");
        }

        /// <summary>The parts evaluation has, to the last place, and a 0 part exactly 0.</summary>
        [Theory]
        [InlineData("x^2", "i")]
        [InlineData("x^(-2)", "i")]
        [InlineData("x^3", "1 + i")]
        [InlineData("x^4", "1 - i")]
        [InlineData("x^7", "2i")]
        [InlineData("x^(-3)", "2i")]
        public void AWholePowerIsAProduct(string expression, string at)
            => BothCompilersGive(expression, at, (expected, actual) => expected == actual);

        /// <summary><c>(pi i)^2</c> is <c>-pi^2</c>, a real number, so a comparison has a truth value.</summary>
        [Theory]
        [InlineData("iverson((x * i)^2 > 0)", "3", 0)]
        [InlineData("iverson((x * i)^2 < 0)", "3", 1)]
        [InlineData("iverson((x * i)^2 < 0)", "pi", 1)]
        [InlineData("iverson((x + x * i)^4 < 0)", "1/2", 1)]
        public void ASquareOfAnImaginaryNumberIsReal(string expression, string at, double truth)
            => BothCompilersGive(expression, at, (expected, actual) => expected == truth && actual == truth);

        [Theory]
        [InlineData("i * x", "+oo")]
        [InlineData("i * x", "-oo")]
        [InlineData("x * (2 + 3i)", "-oo")]
        [InlineData("(x + i) * 2", "-oo")]
        [InlineData("(x - 3i) * (-1/2)", "+oo")]
        [InlineData("ln(x) / 2", "-oo")]
        [InlineData("(x + i) / 4", "+oo")]
        [InlineData("ln(ln(x))", "0")]
        [InlineData("sgn(x)", "+oo")]
        [InlineData("sgn(x)", "-oo")]
        public void AtAnInfinityTheValueIsEvaluations(string expression, string at)
            => BothCompilersGive(expression, at, (expected, actual) => !HasNaN(expected) && expected == actual);

        /// <summary>A number that is 0 times an infinite one has no value, as <c>0 * oo</c> has none.</summary>
        [Theory]
        [InlineData("x * 0", "+oo + i")]
        [InlineData("x * 0", "i * (+oo)")]
        public void ZeroTimesAnInfinityHasNoValue(string expression, string at)
            => BothCompilersGive(expression, at, (expected, actual) => HasNaN(expected) && HasNaN(actual));
    }
}
