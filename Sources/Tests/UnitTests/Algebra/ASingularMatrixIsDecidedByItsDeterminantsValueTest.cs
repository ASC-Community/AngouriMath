//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// The inverse and the negative powers asked whether the determinant was written as <c>0</c>. A
    /// determinant expanded by Laplace, <c>(e^x)^2 - e^(2x)</c> or <c>sin(2x) - 2 sin(x) cos(x)</c>, is zero
    /// without being written so, and the adjugate was divided by it: the inverse was a matrix of
    /// <c>NaN</c>, and <c>M^(-1)</c> simplified to <c>NaN</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2598
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ASingularMatrixIsDecidedByItsDeterminantsValueTest
    {
        public static TheoryData<string> Singular => new()
        {
            "[[e^x, e^(2x)], [1, e^x]]",
            "[[sin(2x), 2 sin(x)], [cos(x), 1]]",
            "[[sin(x)^2, 1], [1 - cos(x)^2, 1]]",
            "[[1, 2], [2, 4]]",
            "[[x, x^2], [1, x]]",
        };

        [Theory]
        [MemberData(nameof(Singular))]
        public void HasNoInverse(string matrix)
            => Assert.Null(((Matrix)matrix.ToEntity()).Inverse);

        [Theory]
        [MemberData(nameof(Singular))]
        public void HasNoNegativePower(string matrix)
            => Assert.Throws<InvalidMatrixOperationException>(() => ((Matrix)matrix.ToEntity()).Pow(-1));

        [Theory]
        [MemberData(nameof(Singular))]
        public void ItsNegativePowerStaysWritten(string matrix)
        {
            // Written as 1 / M, as [[1, 2], [2, 4]]^(-1) is: not a matrix, and not NaN.
            var simplified = MathS.Pow(matrix.ToEntity(), -1).Simplify();
            Assert.IsNotType<Matrix>(simplified);
            Assert.Contains(simplified.Nodes, node => node is Matrix);
            Assert.DoesNotContain(simplified.Nodes, node => node is Number.Complex { IsNaN: true });
        }

        [Theory]
        [MemberData(nameof(Singular))]
        public void DividingByItStaysWritten(string matrix)
        {
            var quotient = "[[1, 0], [0, 1]]".ToEntity() / matrix.ToEntity();
            Assert.Equal(quotient, quotient.InnerSimplified);
        }

        public static TheoryData<string> Invertible => new()
        {
            "[[e^x, e^(2x)], [2, e^x]]",
            "[[sin(2x), sin(x)], [cos(x), 1]]",
            "[[cosh(x), sinh(x)], [sinh(x), cosh(x)]]",
            "[[cos(t), -sin(t)], [sin(t), cos(t)]]",
            "[[a, b], [c, d]]",
        };

        // Each inverse times the matrix is the identity, checked at points on both sides of 0.
        [Theory]
        [MemberData(nameof(Invertible))]
        public void AnInvertibleOneKeepsItsInverse(string written)
        {
            var matrix = (Matrix)written.ToEntity();
            var inverse = Assert.IsAssignableFrom<Matrix>(matrix.Inverse);
            var byPower = matrix.Pow(-1);
            foreach (var product in new[] { matrix * inverse, matrix * byPower })
                foreach (var point in new[] { -1.3, 0.7, 2.1 })
                    for (var row = 0; row < 2; row++)
                        for (var column = 0; column < 2; column++)
                        {
                            var entry = product[row, column];
                            foreach (var name in new[] { "x", "t", "a", "b", "c", "d" })
                                entry = entry.Substitute(name, name switch { "b" => point + 3, "c" => point - 1, _ => point });
                            var value = (Number.Complex)entry.Evaled;
                            var expected = row == column ? 1 : 0;
                            Assert.True((value - expected).Abs() < 1e-12, $"{written} at {point}: [{row}, {column}] is {value}");
                        }
        }
    }
}
