//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The special functions compile, through <c>Compile</c> and <c>Compile&lt;TIn, TOut&gt;</c>
    /// alike, in double precision and in agreement with the interpreter: its branch cuts, and its
    /// values on the axes. So do the factorial, the gamma function and Euler's totient.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1607">#1607</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class SpecialFunctionCompilationTest
    {
        public static readonly TheoryData<string, double, double> Points = new()
        {
            // Near the origin, where erf is its Taylor series, and the others their own series.
            { "erf(x)", 0.3, 0.2 }, { "erf(x)", 2.5, 0 }, { "erf(x)", -1.7, 0 }, { "erf(x)", 1.5, -2.5 },
            { "erf(x)", -3, 4 }, { "erf(x)", 0, 3 }, { "erf(x)", 20, 30 },
            { "erfc(x)", 0.3, 0.2 }, { "erfc(x)", 2.5, 0 }, { "erfc(x)", -1.7, 0 }, { "erfc(x)", 1.5, -2.5 },
            { "erfc(x)", 5, 1 }, { "erfc(x)", 0, 3 },
            { "erfi(x)", 0.3, 0.2 }, { "erfi(x)", 2.5, 0 }, { "erfi(x)", -1.7, 0 }, { "erfi(x)", 1.5, -2.5 },
            // Ei, by its series, by E1's continued fraction, by the asymptotic series, and either side
            // of its cut, where it is i pi more or less than on the axis.
            { "Ei(x)", 0.3, 0.2 }, { "Ei(x)", 2.5, 0 }, { "Ei(x)", -1.7, 0 }, { "Ei(x)", -3, 4 },
            { "Ei(x)", 12, 0 }, { "Ei(x)", -5, 1e-9 }, { "Ei(x)", -5, -1e-9 }, { "Ei(x)", 45, 10 },
            { "Ei(x)", -50, 0 }, { "Ei(x)", 0, 3 },
            { "li(x)", 2.5, 0 }, { "li(x)", 0.4, 0 }, { "li(x)", -2, 0 }, { "li(x)", 1.5, -2.5 }, { "li(x)", 1e6, 0 },
            { "Si(x)", 0.3, 0.2 }, { "Si(x)", 2.5, 0 }, { "Si(x)", -1.7, 0 }, { "Si(x)", 1.5, -2.5 },
            { "Si(x)", 20, 3 }, { "Si(x)", 0, 3 }, { "Si(x)", 50, 0 },
            { "Ci(x)", 0.3, 0.2 }, { "Ci(x)", 2.5, 0 }, { "Ci(x)", -1.7, 0 }, { "Ci(x)", -5, 1e-9 },
            { "Ci(x)", -5, -1e-9 }, { "Ci(x)", 0, 3 }, { "Ci(x)", 0, -3 }, { "Ci(x)", 20, 3 },
            { "Shi(x)", 0.3, 0.2 }, { "Shi(x)", 2.5, 0 }, { "Shi(x)", -1.7, 0 }, { "Shi(x)", 3, 20 },
            { "Chi(x)", 0.3, 0.2 }, { "Chi(x)", 2.5, 0 }, { "Chi(x)", -1.7, 0 }, { "Chi(x)", -5, 1e-9 },
            { "Chi(x)", 3, 20 }, { "Chi(x)", 0, 3 },
        };

        private static NumericsComplex Interpreted(string expression, double re, double im)
        {
            // With the downcasting off, so that a small value is not read as zero.
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            return ((Entity.Number.Complex)expression.ToEntity().Substitute("x", Entity.Number.Complex.Create(re, im)).EvalNumerical()).ToNumerics();
        }

        private static void Close(NumericsComplex expected, NumericsComplex actual)
            => Assert.True((actual - expected).Magnitude <= 1e-12 * expected.Magnitude, $"expected {expected}, got {actual}");

        [Theory]
        [MemberData(nameof(Points))]
        public void TheCompiledFormAgreesWithTheInterpreter(string expression, double re, double im)
            => Close(Interpreted(expression, re, im), expression.ToEntity().Compile("x").Call(new NumericsComplex(re, im)));

        [Theory]
        [MemberData(nameof(Points))]
        public void TheExpressionTreeAgreesWithTheInterpreter(string expression, double re, double im)
            => Close(Interpreted(expression, re, im),
                expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(re, im)));

        /// <summary>
        /// Over doubles, a value where the function is real, and NaN where it is not, as
        /// <see cref="System.Math.Sqrt"/> of a negative number is: <c>li</c>, <c>Ci</c> and <c>Chi</c>
        /// are <c>i pi</c> more than a real value left of 0.
        /// </summary>
        [Theory]
        [InlineData("erf(x)", 2.5)]
        [InlineData("erfc(x)", -1.7)]
        [InlineData("erfi(x)", 2.5)]
        [InlineData("Ei(x)", -1.7)]
        [InlineData("li(x)", 0.4)]
        [InlineData("Si(x)", -1.7)]
        [InlineData("Ci(x)", 2.5)]
        [InlineData("Shi(x)", -1.7)]
        [InlineData("Chi(x)", 2.5)]
        public void OverDoublesARealValueCompiles(string expression, double at)
        {
            var expected = Interpreted(expression, at, 0);
            Assert.Equal(0, expected.Imaginary);
            var compiled = expression.ToEntity().Compile<double, double>("x")(at);
            Assert.True(System.Math.Abs(compiled - expected.Real) <= 1e-12 * System.Math.Abs(expected.Real), $"expected {expected.Real}, got {compiled}");
        }

        /// <summary>
        /// A value too large for a double is infinite in its large part and keeps the part that is
        /// exact on the axis, as the interpreter's does. Halving <c>Ei(z) - Ei(-z)</c> as a complex
        /// number made that part NaN, since an infinity times the other part's zero is one.
        /// </summary>
        [Theory]
        [InlineData("Shi(x)", 1000, 0, double.PositiveInfinity, 0)]
        [InlineData("Shi(x)", -1000, 0, double.NegativeInfinity, 0)]
        [InlineData("Chi(x)", -1000, 0, double.PositiveInfinity, System.Math.PI)]
        [InlineData("Si(x)", 0, 1000, 0, double.PositiveInfinity)]
        [InlineData("Ci(x)", 0, 1000, double.PositiveInfinity, System.Math.PI / 2)]
        public void AnInfiniteValueKeepsItsExactPart(string expression, double re, double im, double expectedRe, double expectedIm)
        {
            var compiled = expression.ToEntity().Compile("x").Call(new NumericsComplex(re, im));
            Assert.Equal(expectedRe, compiled.Real);
            Assert.Equal(expectedIm, compiled.Imaginary);
            var interpreted = Interpreted(expression, re, im);
            Assert.Equal(expectedRe, interpreted.Real);
            Assert.Equal(expectedIm, interpreted.Imaginary, 15);
        }

        [Theory]
        [InlineData("li(x)", -2)]
        [InlineData("Ci(x)", -1.7)]
        [InlineData("Chi(x)", -1.7)]
        public void OverDoublesAValueOffTheRealLineIsNaN(string expression, double at)
            => Assert.True(double.IsNaN(expression.ToEntity().Compile<double, double>("x")(at)));

        /// <summary>
        /// The factorial and the gamma function, through both compilers and over doubles, against
        /// the interpreter. The compiled factorial was NaN from just past 141 to 170, where its power
        /// overflowed before the value did.
        /// </summary>
        [Theory]
        [InlineData("x!", 0.5, 0)]
        [InlineData("x!", -0.5, 0)]
        [InlineData("x!", -2.5, 0)]
        [InlineData("x!", 100.5, 0)]
        [InlineData("x!", 142, 0)]
        [InlineData("x!", 170, 0)]
        [InlineData("x!", 2, 1)]
        [InlineData("x!", -3.5, 2)]
        [InlineData("gamma(x)", 5, 0)]
        [InlineData("gamma(x)", 0.5, 0)]
        [InlineData("gamma(x)", 1.5, -2)]
        public void TheFactorialAgreesWithTheInterpreter(string expression, double re, double im)
        {
            var expected = Interpreted(expression, re, im);
            var at = new NumericsComplex(re, im);
            Close(expected, expression.ToEntity().Compile("x").Call(at));
            Close(expected, expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(at));
            if (im == 0)
                Close(expected, expression.ToEntity().Compile<double, double>("x")(re));
        }

        /// <summary>
        /// A whole number's factorial is the exact integer, rounded once: the compiled one was
        /// <c>0.9999999999999998</c> at 0 and <c>23.999999999999996</c> at 4, which an integer-typed
        /// compilation would have truncated to 23.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(4, 24)]
        [InlineData(20, 2432902008176640000)]
        public void AWholeNumbersFactorialIsExact(long n, long factorial)
        {
            Assert.Equal(factorial, "x!".ToEntity().Compile("x").Call(n).Real);
            Assert.Equal(factorial, "x!".ToEntity().Compile<long, long>("x")(n));
            Assert.Equal(factorial, "gamma(x + 1)".ToEntity().Compile<double, double>("x")(n));
        }

        /// <summary>Past 170 the factorial overflows a double; at a negative whole number, a pole, it has no value.</summary>
        [Theory]
        [InlineData(171, double.PositiveInfinity)]
        [InlineData(-1, double.NaN)]
        [InlineData(-3, double.NaN)]
        public void PastTheLargestDoubleAndAtThePoles(double at, double expected)
        {
            Assert.Equal(expected, "x!".ToEntity().Compile("x").Call(at).Real);
            Assert.Equal(expected, "x!".ToEntity().Compile<double, double>("x")(at));
        }

        /// <summary>
        /// Euler's totient at a whole number, and 0 at one that is not positive, as the interpreter
        /// has it, through both compilers and over every integer type.
        /// </summary>
        [Theory]
        [InlineData(12, 4)]
        [InlineData(1, 1)]
        [InlineData(97, 96)]
        [InlineData(0, 0)]
        [InlineData(-3, 0)]
        public void ThePhiOfAWholeNumber(long n, long phi)
        {
            Assert.Equal(phi, "phi(x)".ToEntity().Compile("x").Call(n).Real);
            Assert.Equal(phi, "phi(x)".ToEntity().Compile<double, double>("x")(n));
            Assert.Equal(phi, "phi(x)".ToEntity().Compile<long, long>("x")(n));
            Assert.Equal(phi, "phi(x)".ToEntity().Compile<int, int>("x")((int)n));
            Assert.Equal(new System.Numerics.BigInteger(phi), "phi(x)".ToEntity().Compile<System.Numerics.BigInteger, System.Numerics.BigInteger>("x")(n));
            // With the downcasting on, which reads the double as the integer it is: with it off, the
            // interpreter's totient sees a decimal and has no value for it.
            Assert.Equal(phi.ToString(), $"phi({n})".ToEntity().EvalNumerical().ToString());
        }

        /// <summary>
        /// Anywhere else the totient has no value, and the interpreter says NaN. The compiler
        /// truncated a fraction to the whole number below it, so <c>phi(2.5)</c> was <c>phi(2)</c>, 1.
        /// </summary>
        [Theory]
        [InlineData(2.5, 0)]
        [InlineData(3, 1)]
        public void ThePhiOfAnyOtherNumberIsNaN(double re, double im)
        {
            Assert.True(double.IsNaN("phi(x)".ToEntity().Compile("x").Call(new NumericsComplex(re, im)).Real));
            Assert.True(double.IsNaN("phi(x)".ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(re, im)).Real));
            if (im == 0)
                Assert.True(double.IsNaN("phi(x)".ToEntity().Compile<double, double>("x")(re)));
        }
    }
}
