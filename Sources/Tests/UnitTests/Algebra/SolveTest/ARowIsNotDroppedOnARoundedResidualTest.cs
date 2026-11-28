//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// <c>y^2 - 2 = 0, x^2 + (P - Q y) x - c = 0</c>, with <c>(1 + sqrt(2))^n = P + Q sqrt(2)</c>, has four solutions. Solved
    /// for <c>y</c> from the second equation, the small root at <c>y = -sqrt(2)</c> is a root of
    /// <c>((x^2 + P x - c)/(Q x))^2 = 2</c> near <c>c / (2P)</c>, which a hundred digits evaluate to a residual rounding made,
    /// with no interval to say so; it was dropped as no root, and three rows were answered. The answer must be all four or
    /// none. https://github.com/asc-community/AngouriMath/issues/2627
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARowIsNotDroppedOnARoundedResidualTest
    {
        [Theory]
        [InlineData(70, 3)]
        [InlineData(80, 5)]
        [InlineData(90, 2)]
        [InlineData(90, 5)]
        [InlineData(100, 2)]
        [InlineData(120, 2)]
        [InlineData(120, 3)]
        [InlineData(120, 5)]
        public void IsAnsweredWithEveryRowOrNone(int n, int c)
        {
            System.Numerics.BigInteger p = 1, q = 0;
            for (var i = 0; i < n; i++)
                (p, q) = (p + 2 * q, p + q);
            var equations = new[] { "y^2 - 2".ToEntity(), $"x^2 + ({p} - {q} * y) * x - {c}".ToEntity() };
            var solutions = MathS.Equations(equations).Solve("x", "y");
            if (solutions is null)
                return;
            Assert.Equal(4, solutions.RowCount);
            for (var row = 0; row < solutions.RowCount; row++)
                foreach (var equation in equations)
                {
                    var residual = equation.Substitute("x", solutions[row, 0]).Substitute("y", solutions[row, 1]);
                    var enclosure = AngouriMath.Numerics.PreciseEvaluation.Of(residual, 300);
                    Assert.True(enclosure is { } value
                        && value.Re.Magnitude.CompareTo(PeterO.Numbers.EDecimal.FromString("1E-60")) <= 0
                        && value.Im.Magnitude.CompareTo(PeterO.Numbers.EDecimal.FromString("1E-60")) <= 0,
                        $"row {row} leaves {residual.Evaled} in {equation}");
                }
        }
    }
}
