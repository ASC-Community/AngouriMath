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
    /// The conjugate, the principal argument and the real and the imaginary parts compile, through
    /// <c>Compile</c> and <c>Compile&lt;TIn, TOut&gt;</c> alike, to the values the interpreter gives;
    /// and an Iverson bracket compiles to a number over the reals.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ComplexPartsCompilationTest
    {
        /// <summary>
        /// Off the axes, on each half-axis, and at -1 with a negative zero, where the principal
        /// argument is pi and Math.Atan2 would give -pi.
        /// </summary>
        public static readonly TheoryData<string, double, double> Points = new()
        {
            { "conj(x)", 0.3, 0.2 }, { "conj(x)", -2.5, -1.5 }, { "conj(x)", 4, 0 },
            { "arg(x)", 0.3, 0.2 }, { "arg(x)", -2.5, -1.5 }, { "arg(x)", -1, 0 }, { "arg(x)", -1, -0.0 },
            { "arg(x)", 0, 3 }, { "arg(x)", 0, -3 }, { "arg(x)", 4, 0 },
            { "Re(x)", 0.3, 0.2 }, { "Re(x)", -2.5, -1.5 }, { "Im(x)", 0.3, 0.2 }, { "Im(x)", -2.5, -1.5 },
            { "Re(x) * Im(x) + conj(x) * arg(x)", 1.25, -0.75 },
        };

        private static NumericsComplex Interpreted(string expression, double re, double im)
            => ((Entity.Number.Complex)expression.ToEntity().Substitute("x", Entity.Number.Complex.Create(re, im)).EvalNumerical()).ToNumerics();

        private static void Close(NumericsComplex expected, NumericsComplex actual)
            => Assert.True((actual - expected).Magnitude <= 1e-12 * System.Math.Max(1, expected.Magnitude), $"expected {expected}, got {actual}");

        [Theory]
        [MemberData(nameof(Points))]
        public void TheCompiledFormAgreesWithTheInterpreter(string expression, double re, double im)
            => Close(Interpreted(expression, re, im), expression.ToEntity().Compile("x").Call(new NumericsComplex(re, im)));

        [Theory]
        [MemberData(nameof(Points))]
        public void TheExpressionTreeAgreesWithTheInterpreter(string expression, double re, double im)
            => Close(Interpreted(expression, re, im),
                expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(re, im)));

        /// <summary>The argument of 0 has no value, compiled or interpreted.</summary>
        [Fact]
        public void TheArgumentOfZeroHasNoValue()
        {
            Assert.True(double.IsNaN("arg(x)".ToEntity().Compile("x").Call(0).Real));
            Assert.True(double.IsNaN("arg(x)".ToEntity().Compile<double, double>("x")(0)));
            Assert.Equal(MathS.NaN, "arg(0)".ToEntity().Evaled);
        }

        /// <summary>
        /// The parts of a value that is not there are not there either. <c>x / x</c> at 0 compiles
        /// to <c>NaN + NaN i</c>, its imaginary part to <c>NaN</c>, and the imaginary part of that
        /// was <c>0</c>, a value where the interpreter has none.
        /// </summary>
        [Theory]
        [InlineData("Im(Im(x / x))")]
        [InlineData("Im(Re(x / x))")]
        [InlineData("Re(Im(x / x))")]
        [InlineData("Im(Im(sqrt(x) / sqrt(x)))")]
        public void APartOfNoValueHasNoValue(string expression)
        {
            Assert.True(expression.ToEntity().Substitute("x", 0).Evaled.IsNaN);
            Assert.True(double.IsNaN(expression.ToEntity().Compile("x").Call(0).Real), $"FastExpression gives {expression.ToEntity().Compile("x").Call(0)}");
            Assert.True(double.IsNaN(expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(0).Real),
                $"LINQ gives {expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(0)}");
        }

        /// <summary>A NaN in either part of an argument is the whole argument's.</summary>
        [Theory]
        [InlineData("Re(x)", 1, double.NaN)]
        [InlineData("Im(x)", double.NaN, 1)]
        public void ANaNInOnePartIsTheWholeNumbers(string expression, double re, double im)
        {
            Assert.True(double.IsNaN(expression.ToEntity().Compile("x").Call(new NumericsComplex(re, im)).Real));
            Assert.True(double.IsNaN(expression.ToEntity().Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(re, im)).Real));
        }

        /// <summary>Over doubles, the argument of a negative number is pi, and the imaginary part is 0.</summary>
        [Theory]
        [InlineData("arg(x)", -2, System.Math.PI)]
        [InlineData("arg(x)", 2, 0)]
        [InlineData("Re(x)", -2, -2)]
        [InlineData("Im(x)", -2, 0)]
        [InlineData("conj(x)", -2, -2)]
        [InlineData("iverson(x > 0)", 2, 1)]
        [InlineData("iverson(x > 0)", -2, 0)]
        [InlineData("iverson(x = 2) * x", 2, 2)]
        public void OverTheRealsTheyAreReal(string expression, double at, double expected)
            => Assert.Equal(expected, expression.ToEntity().Compile<double, double>("x")(at), 12);
    }
}
