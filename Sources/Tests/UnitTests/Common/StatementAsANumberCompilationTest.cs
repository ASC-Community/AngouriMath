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
    /// In the LINQ compiler a statement asked for as a number is its truth value, 1, 0 or NaN, as in FastExpression,
    /// and a provided or a piecewise statement has no truth value where no case holds. Each threw
    /// <c>InvalidProtocolProvided</c>, there being no NaN of a bool.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2224">#2224</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class StatementAsANumberCompilationTest
    {
        private static readonly Complex[] points =
            { -2, -0.5, 0, 0.5, 1, 2, 3, 6, Complex.ImaginaryOne, new Complex(1, 1), double.NaN, double.PositiveInfinity };

        private static bool Same(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : expected == actual;

        [Theory]
        [InlineData("x > 1")]
        [InlineData("x = 2")]
        [InlineData("not x > 1")]
        [InlineData("x > 0 and x < 2")]
        [InlineData("x in [0; 1]")]
        [InlineData("3 divides x")]
        [InlineData("x > 1 provided x > 0")]
        [InlineData("piecewise(x > 1 provided x > 0, x < -1)")]
        public void AsANumberAStatementIsFastExpressionsTruthValue(string statement)
        {
            var fast = statement.ToEntity().Compile("x");
            var complex = statement.ToEntity().Compile<Complex, Complex>("x");
            var real = statement.ToEntity().Compile<double, double>("x");
            foreach (var x in points)
            {
                Assert.True(Same(fast.Call(x), complex(x)), $"{statement} at {x}: {fast.Call(x)} by FastExpression, {complex(x)} by LINQ");
                if (x.Imaginary == 0)
                    Assert.True(Same(fast.Call(x), real(x.Real)), $"{statement} at {x}: {fast.Call(x)} by FastExpression, {real(x.Real)} over double");
            }
        }

        [Theory]
        [InlineData("x > 1 provided x > 0", 2, true)]
        [InlineData("x > 1 provided x > 0", 0.5, false)]
        [InlineData("x > 1 provided x > 0", -1, null)]
        [InlineData("piecewise(x > 1 provided x > 0, x < -1)", 0.5, false)]
        [InlineData("piecewise(x > 1 provided x > 0, x < -1)", -2, true)]
        [InlineData("piecewise(x > 1 provided x > 0, x < -1)", -0.5, false)]
        [InlineData("piecewise(x > 1 provided x > 0, x < -1 provided x < 0)", 0, null)]
        public void AProvidedOrPiecewiseStatementHasTheTruthValueOfItsCase(string statement, double x, bool? expected)
            => Assert.Equal(expected, statement.ToEntity().Compile<double, bool?>("x")(x));

        [Theory]
        [InlineData("piecewise(x > 1 provided x > 0, x)")]
        public void APiecewiseOfStatementsAndNumbersIsRefused(string expression)
            => Assert.Throws<UncompilableNodeException>(() => expression.ToEntity().Compile<double, double>("x"));

        /// <summary>A statement that may have no truth value has none as an integer type, which has no NaN.</summary>
        [Fact]
        public void AsAnIntegerAStatementThatMayHaveNoTruthValueIsRefused()
            => Assert.Throws<UncompilableNodeException>(() => "x > 1".ToEntity().Compile<double, long>("x"));
    }
}
