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
    /// The LINQ compiler gives what evaluation and FastExpression give, where it did not: a constant whose value is
    /// not real, over <c>Complex</c> (<a href="https://github.com/asc-community/AngouriMath/issues/2235">#2235</a>); an
    /// Iverson bracket or a provided integer inside a function
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/2237">#2237</a>); and <c>sgn(0)</c>
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/2238">#2238</a>).
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class LinqCompilationValueTest
    {
        private static Complex Evaluated(string expression, Complex x)
            => ((Entity.Number.Complex)expression.ToEntity().Substitute("x", (Entity.Number.Complex)x).Evaled).ToNumerics();

        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : Complex.Abs(expected - actual) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        // A constant whose value is not real: it was taken over double, and NaN.
        [InlineData("x + sqrt(-1)", 0)]
        [InlineData("x + ln(-1)", 0)]
        [InlineData("x + arcsin(2)", 0)]
        [InlineData("x + arccos(-2)", 1)]
        [InlineData("x * sqrt(-4)", 1)]
        [InlineData("x + (-8)^(1/4)", 0)]
        // 1/0 has no value, where over double it was +oo.
        [InlineData("x + 1/0", 0)]
        // An Iverson bracket and a provided integer inside a function.
        [InlineData("x + tan(iverson(pi > 0))", 1)]
        [InlineData("x + abs(iverson(e > 3))", 1)]
        [InlineData("x + floor(iverson(x > 0))", 1)]
        [InlineData("x + sgn(-1 provided pi > 3)", 1)]
        [InlineData("max(iverson(x > 0), 1/2) + x", -1)]
        // sgn(0) is 0.
        [InlineData("sgn(x)", 0)]
        [InlineData("sgn(x - x)", 1)]
        [InlineData("sgn(x)", -3)]
        public void OverComplexItIsWhatEvaluationAndFastExpressionGive(string expression, double at)
        {
            var expected = Evaluated(expression, at);
            var linq = expression.ToEntity().Compile<Complex, Complex>("x")(at);
            var fast = expression.ToEntity().Compile("x").Call(at);
            Assert.True(Agree(expected, linq), $"{expression} at {at}: {expected} evaluated, {linq} by LINQ");
            Assert.True(Agree(expected, fast), $"{expression} at {at}: {expected} evaluated, {fast} by FastExpression");
        }

        /// <summary>A constant is taken over Complex where the result is one, whatever the argument's type.</summary>
        [Fact]
        public void ToAComplexResultAConstantIsComplex()
            => Assert.True(Agree(new Complex(2, 1), "x + sqrt(-1)".ToEntity().Compile<double, Complex>("x")(2)));

        /// <summary>Over double a value that is not real has none, as before.</summary>
        [Theory]
        [InlineData("x + sqrt(-1)", 0)]
        [InlineData("x + ln(-1)", 0)]
        public void OverDoubleAValueThatIsNotRealIsNaN(string expression, double at)
            => Assert.True(double.IsNaN(expression.ToEntity().Compile<double, double>("x")(at)));

        [Theory]
        [InlineData("x + tan(iverson(pi > 0))", 1)]
        [InlineData("x + floor(iverson(x > 0))", 1)]
        [InlineData("x + sgn(-1 provided pi > 3)", 1)]
        public void OverDoubleANullableOperandIsWhatEvaluationGives(string expression, double at)
            => Assert.True(Agree(Evaluated(expression, at), expression.ToEntity().Compile<double, double>("x")(at)));
    }
}
