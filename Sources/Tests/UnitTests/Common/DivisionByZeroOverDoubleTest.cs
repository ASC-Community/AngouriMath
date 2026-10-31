//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Compilation.IntoLinq;
using AngouriMath.Extensions;
using GenericTensor.Core;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Compiled over <see cref="double"/>, a division by zero, and 0 to a power that is not positive, have no value, as in
    /// evaluation, FastExpression and the compiler over <see cref="System.Numerics.Complex"/>: the IEEE quotient made 1/0
    /// +oo, and <see cref="System.Math.Pow(double, double)"/> made 0^(-1) +oo and 0^0 1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2286">#2286</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class DivisionByZeroOverDoubleTest
    {
        [Theory]
        [InlineData("1/x", 0)]
        [InlineData("-1/x", 0)]
        [InlineData("1/(x - x)", 3)]
        [InlineData("x / floor(x)", 0.5)]
        [InlineData("x^(-1)", 0)]
        [InlineData("x^(-1/2)", 0)]
        [InlineData("x^x", 0)]
        public void ItHasNoValue(string expression, double at)
        {
            var overDoubles = expression.ToEntity().Compile<double, double>("x")(at);
            Assert.True(double.IsNaN(overDoubles), $"{expression} at {at}: {overDoubles} over double");
            Assert.True(float.IsNaN(expression.ToEntity().Compile<float, float>("x")((float)at)), $"{expression} at {at} over float");
            Assert.True(expression.ToEntity().Substitute("x", at).Evaled.IsNaN, $"{expression} at {at} evaluates to a value");
        }

        /// <summary>
        /// A matrix argument divided by a zero scalar, entry by entry as a scalar is. A matrix written into the expression
        /// is divided before it is compiled, and only an argument reaches the compiled matrix division.
        /// </summary>
        [Fact]
        public void AMatrixOverZeroHasNoEntries()
        {
            var matrix = GenTensor<double, DoubleOperations>.CreateMatrix(new double[,] { { 1, 2 }, { 3, 4 } });
            var divided = "A / x".ToEntity()
                .Compile<GenTensor<double, DoubleOperations>, double, GenTensor<double, DoubleOperations>>("A", "x")(matrix, 0);
            for (var i = 0; i < 2; i++)
                for (var j = 0; j < 2; j++)
                    Assert.True(double.IsNaN(divided[i, j]), $"[{i}, {j}] is {divided[i, j]}");
        }

        [Theory]
        [InlineData("1/x", 4, 0.25)]
        [InlineData("x^(1/2)", 0, 0)]
        [InlineData("x^2", 0, 0)]
        [InlineData("0 / x", 2, 0)]
        public void WhereItHasOneItIsThatValue(string expression, double at, double value)
            => Assert.Equal(value, expression.ToEntity().Compile<double, double>("x")(at));
    }
}
