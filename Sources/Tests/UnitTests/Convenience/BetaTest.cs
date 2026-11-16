//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The beta function <c>B(a, b) = Γ(a) Γ(b)/Γ(a + b)</c> of DLMF 5.12.1. <c>beta(a, b)</c> was a parse error.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class BetaTest
    {
        private static Number.Complex Point(string re, string im)
            => Number.Complex.Create(EDecimal.FromString(re), EDecimal.FromString(im));

        private static void AssertClose(Entity expected, Entity actual, string tolerance, string what)
        {
            var (want, value) = (Assert.IsAssignableFrom<Number.Complex>(expected.EvalNumerical()), Assert.IsAssignableFrom<Number.Complex>(actual.EvalNumerical()));
            var error = ((Number.Complex)(value - want).EvalNumerical()).Abs().EDecimal;
            Assert.True(error.CompareTo(want.Abs().EDecimal.Multiply(EDecimal.FromString(tolerance))) <= 0,
                $"{what} = {value.Stringize()}, expected {want.Stringize()}");
        }

        /// <summary>
        /// mpmath's <c>beta(a, b)</c> at 80 digits, written to 62: positive, negative and mixed real arguments, between the poles,
        /// off the real line, near 0, and large, where each gamma function is far past the exponents of the working
        /// precision while the ratio is not.
        /// </summary>
        public static readonly TheoryData<string, string, string, string, string, string> Points = new()
        {
            { "2.5", "0", "1.3", "0", "0.25415419017646049453907446008147409307122226751762955916235399", "0" },
            { "0.3", "0", "-2.5", "0", "1.2825311735701476526986843538242604345907029343507235507733304", "0" },
            { "-1.7", "0", "-2.2", "0", "-11.268727037795777568525454116885162290249895194669459478307997", "0" },
            { "2", "1", "1", "-1", "0.13601452749106658147511829183601877792035918173015797475344839", "0.13601452749106658147511829183601877792035918173015797475344839" },
            { "-3.5", "0.5", "0.25", "-2", "-0.22695054733851731161121813808369437012066413892202907929162645", "-1.0885855535208685897504747934926043526415191185660330242817783" },
            { "50.5", "0", "30.25", "0", "3.7026748324957166069870150508681033575250016720748845887261983e-24", "0" },
            { "0.001", "0", "0.002", "0", "1499.9950759984829321613625968312663178979891737743132762648375", "0" },
            { "-0.5", "0", "3.7", "0", "-6.0993342324110613820369031173464486614722363532952898498567362", "0" },
            { "300.5", "0", "200.25", "0", "1.0148207207762698096918501681756088688794963678294382380582536e-147", "0" },
            { "-7.25", "3", "4.5", "-0.75", "0.00032918588243885326188531199277690762907893462845786465804540795", "0.00032791329090914045764071320115971561560740164276304850496897522" },
        };

        [Theory]
        [MemberData(nameof(Points))]
        public void ItAgreesWithMpmath(string aRe, string aIm, string bRe, string bIm, string expectedRe, string expectedIm)
            => AssertClose(Point(expectedRe, expectedIm), MathS.Beta(Point(aRe, aIm), Point(bRe, bIm)), "1e-58",
                $"beta({aRe} + {aIm}i, {bRe} + {bIm}i)");

        /// <summary>At 200 digits it is the ratio of the library's own gamma functions to the precision.</summary>
        [Theory]
        [InlineData("2.7", "-0.35")]
        [InlineData("-4.6", "1.2")]
        [InlineData("1.5 + 2i", "0.5 - i")]
        [InlineData("-2.25 - 0.5i", "-3.75 + 4i")]
        [InlineData("1/3", "17/7")]
        public void ItIsTheRatioOfTheGammaFunctions(string a, string b)
        {
            using var _ = MathS.Settings.DecimalPrecisionContext.Set(new EContext(200, ERounding.HalfEven, -100000, 100000, false));
            var (av, bv) = (a.ToEntity(), b.ToEntity());
            AssertClose(MathS.Gamma(av) * MathS.Gamma(bv) / MathS.Gamma(av + bv), MathS.Beta(av, bv), "1e-195", $"beta({a}, {b})");
        }

        /// <summary>
        /// Exact where the gamma functions are: a whole argument, <c>B(n, b) = (n - 1)!/(b (b + 1) ... (b + n - 1))</c>,
        /// and two half-whole ones, a rational times <c>pi</c>. Where only <c>a + b</c> is <c>0, -1, -2, ...</c> it is
        /// <c>0</c>, and where one argument is as well, the rational function above at that point.
        /// </summary>
        [Theory]
        [InlineData("beta(2, 3)", "1/12")]
        [InlineData("beta(1, 1)", "1")]
        [InlineData("beta(5, 7)", "1/2310")]
        [InlineData("beta(1/2, 1/2)", "pi")]
        [InlineData("beta(1/2, 3/2)", "pi/2")]
        [InlineData("beta(5/2, -3/2)", "pi")]
        [InlineData("beta(-1/2, 7/2)", "-15 pi/8")]
        [InlineData("beta(1, 1/2)", "2")]
        [InlineData("beta(3, 1/3)", "27/14")]
        [InlineData("beta(2, -1/2)", "-4")]
        [InlineData("beta(1/2, -1/2)", "0")]
        [InlineData("beta(-1/2, -1/2)", "0")]
        [InlineData("beta(1/3, -4/3)", "0")]
        [InlineData("beta(1, -1)", "-1")]
        [InlineData("beta(2, -3)", "1/6")]
        [InlineData("beta(-3, 2)", "1/6")]
        [InlineData("beta(3, -5)", "-1/30")]
        public void ItIsExactWhereTheValueIsKnown(string written, string expected)
        {
            var exact = written.ToEntity().Simplify();
            Assert.Equal(Number.Integer.Zero, (exact - expected.ToEntity()).Simplify());
            // And the same as evaluation, which takes the poles' cases apart from the gamma functions.
            AssertClose(expected.ToEntity() + 1, written.ToEntity().Evaled + 1, "1e-90", written);
        }

        /// <summary>
        /// Away from the poles, the exact value is the ratio of the gamma functions, which this checks it against, at a
        /// whole argument and at two half-whole ones.
        /// </summary>
        [Theory]
        [InlineData("3", "1/3")]
        [InlineData("4", "-7/2")]
        [InlineData("5/2", "-3/2")]
        [InlineData("-1/2", "7/2")]
        [InlineData("11/2", "13/2")]
        public void TheExactValueIsTheGammaRatio(string a, string b)
        {
            var (av, bv) = (a.ToEntity(), b.ToEntity());
            AssertClose(MathS.Gamma(av) * MathS.Gamma(bv) / MathS.Gamma(av + bv), MathS.Beta(av, bv).Simplify(), "1e-95", $"beta({a}, {b})");
        }

        /// <summary>
        /// <c>B(n, b)</c> for a whole <c>n</c> is a rational function of <c>b</c>, so where <c>b</c> is a pole and
        /// <c>n + b</c> is too, the value is the function's, which is the limit of <c>B(n, b)</c> as <c>b</c> approaches.
        /// </summary>
        [Theory]
        [InlineData(1, -1)]
        [InlineData(2, -3)]
        [InlineData(3, -5)]
        [InlineData(4, -4)]
        public void AtABetweenPolesItIsTheLimitAlongAWholeN(int n, int b)
        {
            Entity near = Number.Rational.Create(EInteger.FromInt32(b) * EInteger.FromInt32(10).Pow(30) + 1, EInteger.FromInt32(10).Pow(30));
            AssertClose(MathS.Beta(n, near), MathS.Beta(n, b).Simplify(), "1e-25", $"beta({n}, {b})");
        }

        /// <summary>A pole where it is not cancelled: an argument at one and the sum not, or both arguments.</summary>
        [Theory]
        [InlineData("beta(0, 1/2)")]
        [InlineData("beta(0, 0)")]
        [InlineData("beta(1, 0)")]
        [InlineData("beta(-2, 3/2)")]
        [InlineData("beta(-1, -1)")]
        [InlineData("beta(-2, 5)")]
        [InlineData("beta(-3, 1 + i)")]
        [InlineData("beta(-2.0, 1.5)")]
        public void ThereIsNoValueAtAPole(string written)
        {
            Assert.Equal(MathS.NaN, written.ToEntity().Simplify());
            Assert.Equal(MathS.NaN, written.ToEntity().Evaled);
        }

        [Fact]
        public void TheDomainExcludesThePolesItDoesNotCancel()
        {
            var condition = "beta(a, b)".ToEntity().DomainCondition;
            Entity At(Entity a, Entity b) => condition.Substitute("a", a).Substitute("b", b).Simplify();
            Assert.Equal(Boolean.False, At(0, MathS.FromString("1/2")));
            Assert.Equal(Boolean.False, At(-1, -1));
            Assert.Equal(Boolean.False, At(-2, 5));
            Assert.Equal(Boolean.True, At(2, -3));
            Assert.Equal(Boolean.True, At(MathS.FromString("1/2"), MathS.FromString("-1/2")));
            Assert.Equal(Boolean.True, At(MathS.FromString("5/2"), MathS.FromString("-3/2")));
            Assert.Equal(Boolean.True, At(3, 4));
        }

        /// <summary><c>B(1, b) = 1/b</c>, and <c>B(a, b) = B(b, a)</c>, each kept by simplification.</summary>
        [Fact]
        public void ItIsSymmetricAndOneOverBAtOne()
        {
            Assert.Equal("1 / b".ToEntity(), "beta(1, b)".ToEntity().Simplify());
            Assert.Equal("1 / b".ToEntity(), "beta(b, 1)".ToEntity().Simplify());
            Assert.Equal("beta(a, b)".ToEntity().Simplify(), "beta(b, a)".ToEntity().Simplify());
            Assert.Equal("beta(x^2, sin(x))".ToEntity().InnerSimplified, "beta(sin(x), x^2)".ToEntity().InnerSimplified);
            var (a, b) = (MathS.FromString("0.7 - 2i"), MathS.FromString("-1.3 + 0.4i"));
            AssertClose(MathS.Beta(a, b), MathS.Beta(b, a), "1e-95", "beta(b, a)");
        }

        /// <summary>
        /// <c>dB/da = B(a, b) (psi(a) - psi(a + b))</c>, from <c>ln B = ln Γ(a) + ln Γ(b) - ln Γ(a + b)</c>, checked
        /// against a central difference in each argument, off the real line and between the poles.
        /// </summary>
        [Theory]
        [InlineData("a", "1.3 + 0.4i", "-0.6 - i")]
        [InlineData("b", "1.3 + 0.4i", "-0.6 - i")]
        [InlineData("a", "-2.7", "0.45")]
        [InlineData("b", "-2.7", "0.45")]
        public void ItsDerivativeIsTheBetaFunctionTimesADifferenceOfDigammas(string variable, string a, string b)
        {
            var (av, bv) = (MathS.FromString(a), MathS.FromString(b));
            var derivative = "beta(a, b)".ToEntity().Differentiate(variable).Substitute("a", av).Substitute("b", bv);
            Entity h = Number.Rational.Create(1, EInteger.FromInt32(10).Pow(20));
            var difference = variable == "a"
                ? (MathS.Beta(av + h, bv) - MathS.Beta(av - h, bv)) / (2 * h)
                : (MathS.Beta(av, bv + h) - MathS.Beta(av, bv - h)) / (2 * h);
            AssertClose(difference, derivative, "1e-30", $"d/d{variable} beta({a}, {b})");
        }

        /// <summary><c>beta</c> alone is a variable, and with a space before the bracket it is a product, as <c>gamma</c> is.</summary>
        [Theory]
        [InlineData("beta", "beta")]
        [InlineData("beta (a, b)", null)]
        [InlineData("beta (x)", "beta * x")]
        [InlineData("gamma (x)", "gamma * x")]
        // With one argument it is the product it was before beta(a, b) was a function.
        [InlineData("beta(x)", "beta * x")]
        [InlineData("beta(x)^2", "beta * x ^ 2")]
        [InlineData("2^beta(x)", "2 ^ beta * x")]
        [InlineData("beta(log(2, x))", "beta * log(2, x)")]
        [InlineData("beta([1, 2])", "beta * [1, 2]")]
        public void TheNameWithoutTwoArgumentsIsWhatItWas(string written, string? expected)
        {
            if (expected is null)
                Assert.ThrowsAny<ParseException>(() => written.ToEntity());
            else
                Assert.Equal(expected, written.ToEntity().Stringize());
        }

        [Fact]
        public void ItTakesTwoArguments()
        {
            Assert.Throws<FunctionArgumentCountException>(() => "beta(a, b, c)".ToEntity());
            Assert.IsType<Betaf>("beta(beta(a, b), [[1, 2]][1, 2])".ToEntity());
        }

        [Fact]
        public void ASymbolKeepsTheNode()
            => Assert.IsType<Betaf>("beta(a, b)".ToEntity().Simplify());

        [Theory]
        [InlineData("beta(a, b)", "beta(a, b)", @"\operatorname{B}\left(a, b\right)", "sympy.beta(a, b)")]
        [InlineData("beta(a + 1, b ^ 2)", "beta(a + 1, b ^ 2)", @"\operatorname{B}\left(a+1, {b}^{2}\right)", "sympy.beta(a + 1, b ** 2)")]
        public void ItIsWrittenAsBeta(string written, string stringized, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(stringized, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }

        /// <summary>
        /// It compiles, as the gamma function does, in FastExpression and in the LINQ compiler over complex numbers and
        /// over doubles, to what evaluation gives: at whole, half-whole, negative and complex arguments, between the
        /// poles, at the poles, where only the sum is one, and where each gamma function is past the doubles.
        /// </summary>
        [Fact]
        public void ItCompilesToWhatEvaluationGives()
        {
            var points = new[] { "1", "2", "5", "0.5", "1.5", "-0.5", "-1.5", "0", "-1", "-3", "2.75", "-2.6", "170", "300.25", "1 + i", "-0.5 + 2i", "0.3 - 0.1i" };
            var fast = "beta(a, b)".ToEntity().Compile("a", "b");
            var linq = "beta(a, b)".ToEntity().Compile<NumericsComplex, NumericsComplex, NumericsComplex>("a", "b");
            var real = "beta(a, b)".ToEntity().Compile<double, double, double>("a", "b");
            foreach (var a in points)
                foreach (var b in points)
                {
                    var evaluated = Assert.IsAssignableFrom<Number.Complex>($"beta({a}, {b})".ToEntity().Evaled).ToNumerics();
                    var (x, y) = (((Number.Complex)a.ToEntity().Evaled).ToNumerics(), ((Number.Complex)b.ToEntity().Evaled).ToNumerics());
                    Assert.True(Agree(evaluated, fast.Call(x, y)), $"beta({a}, {b}): {evaluated} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(Agree(evaluated, linq(x, y)), $"beta({a}, {b}): {evaluated} evaluated, {linq(x, y)} by LINQ");
                    if (x.Imaginary == 0 && y.Imaginary == 0)
                        Assert.True(Agree(evaluated, real(x.Real, y.Real)), $"beta({a}, {b}): {evaluated} evaluated, {real(x.Real, y.Real)} over double");
                }
        }

        // Both NaN, or within a part in ten billion, or both under the doubles.
        private static bool Agree(NumericsComplex expected, NumericsComplex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
               ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
               : NumericsComplex.Abs(expected - actual) <= 1e-10 * NumericsComplex.Abs(expected)
                 || NumericsComplex.Abs(expected) < 1e-300 && NumericsComplex.Abs(actual) < 1e-300;

        /// <summary>Entry by entry over a matrix, as the other functions of two numbers are.</summary>
        [Fact]
        public void ItIsTakenEntryByEntryOverAMatrix()
            => Assert.Equal("[1/3, 1/12]".ToEntity(), "beta([1, 2], 3)".ToEntity().Simplify());
    }
}
