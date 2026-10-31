//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Compiled over <see cref="double"/>, a value that is not real is NaN, and so is everything computed from it, though
    /// the expression may be real there: sin(arcsin(x)) at 2 is NaN. That is documented, and
    /// <see cref="MathS.Settings.RefuseNonRealCompilation"/> refuses such an expression instead, so that a NaN means only
    /// that there is no value.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2286">#2286</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class RefuseNonRealCompilationTest
    {
        [Theory]
        [InlineData("sin(arcsin(x))", 2)]
        [InlineData("sqrt(x)^2", -4)]
        public void ByDefaultAValueOffTheRealsIsNaN(string expression, double at)
            => Assert.True(double.IsNaN(expression.Compile<double, double>("x")(at)));

        [Theory]
        [InlineData("sin(arcsin(x))")]
        [InlineData("sqrt(x)")]
        [InlineData("ln(x)")]
        [InlineData("x^(1/3)")]
        public void WhenRefusingWhatIsNotRealItIsRefused(string expression)
        {
            using var _ = MathS.Settings.RefuseNonRealCompilation.Set(true);
            Assert.Throws<UncompilableNodeException>(() => expression.Compile<double, double>("x"));
            Assert.Throws<UncompilableNodeException>(() => expression.Compile<float, float>("x"));
        }

        [Fact]
        public void WhenRefusingAPowerOfTwoArgumentsIsRefused()
        {
            using var _ = MathS.Settings.RefuseNonRealCompilation.Set(true);
            Assert.Throws<UncompilableNodeException>(() => "x^y".Compile<double, double, double>("x", "y"));
        }

        [Theory]
        [InlineData("x / (1 + x^2)", 2, 0.4)]
        [InlineData("floor(x) + max(x, 1)", 2.5, 4.5)]
        [InlineData("abs(x) + e^x - e^x", -3, 3)]
        [InlineData("x^3 - 2 * x", 2, 4)]
        public void WhenRefusingWhatIsRealIsCompiled(string expression, double at, double value)
        {
            using var _ = MathS.Settings.RefuseNonRealCompilation.Set(true);
            Assert.Equal(value, expression.Compile<double, double>("x")(at), 12);
        }

        [Fact]
        public void WhenRefusingADivisionByZeroIsStillNaN()
        {
            using var _ = MathS.Settings.RefuseNonRealCompilation.Set(true);
            Assert.True(double.IsNaN("1/x".Compile<double, double>("x")(0)));
        }

        [Fact]
        public void WhenRefusingTheComplexCompilerAndAStatementAreUnaffected()
        {
            using var _ = MathS.Settings.RefuseNonRealCompilation.Set(true);
            Assert.Equal(new Complex(2, 0), "sqrt(x)".Compile<Complex, Complex>("x")(4));
            Assert.True("sqrt(x) > 0".Compile<double, bool>("x")(4));
        }
    }
}
