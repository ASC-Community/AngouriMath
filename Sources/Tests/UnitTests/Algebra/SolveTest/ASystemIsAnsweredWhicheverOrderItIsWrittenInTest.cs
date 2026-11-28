//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// The square eliminator took the last unknown from the first equation that gave it a root, and kept
    /// whatever that led to. From <c>ln(x) + ln(y) = 1</c>, <c>y</c> is <c>e^(1 - ln(x))</c>, and
    /// <c>x = e^(1 - ln(x))</c> is solved by Newton's method, so the answer was a decimal; and where the
    /// substitution left an equation it could not solve, the system was declined. Written the other way
    /// round, <c>y = x</c> and each is exact.
    /// https://github.com/asc-community/AngouriMath/issues/2607
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ASystemIsAnsweredWhicheverOrderItIsWrittenInTest
    {
        private static Matrix Solve(string first, string second)
        {
            var solved = MathS.Equations(first.ToEntity(), second.ToEntity()).Solve("x", "y");
            Assert.NotNull(solved);
            return solved!;
        }

        [Theory]
        [InlineData("ln(x) + ln(y) - 1", "x - y", "sqrt(e)")]
        [InlineData("ln(x) + ln(y) - 2", "x - y", "e")]
        [InlineData("x - y", "ln(x) + ln(y) - 1", "sqrt(e)")]
        public void IsExact(string first, string second, string root)
        {
            var solutions = Solve(first, second);
            Assert.DoesNotContain(solutions.Nodes, node => node is Number { IsExact: false });
            Assert.Equal(1, solutions.RowCount);
            for (var column = 0; column < 2; column++)
                Assert.Equal(Number.Integer.Zero, (solutions[0, column] - root.ToEntity()).Simplify());
        }

        // Each row holds in both equations at the whole numbers n_1 = -1, 0, 2, and the real root is
        // among them.
        [Theory]
        [InlineData("e^x * e^y - 4", "x - y", "ln(2)")]
        [InlineData("2^x + 2^y - 8", "x - y", "2")]
        public void IsAnsweredRatherThanDeclined(string first, string second, string realRoot)
        {
            var solutions = Solve(first, second);
            var found = false;
            for (var row = 0; row < solutions.RowCount; row++)
                foreach (var n in new[] { -1, 0, 2 })
                {
                    var x = solutions[row, 0].Substitute("n_1", n);
                    var y = solutions[row, 1].Substitute("n_1", n);
                    foreach (var equation in new[] { first, second })
                    {
                        var value = (Number.Complex)equation.ToEntity().Substitute("x", x).Substitute("y", y).Evaled;
                        Assert.True(((Number.Real)value.Abs()).EDecimal.ToDouble() < 1e-9, $"{equation} at row {row}, n_1 = {n}: {value}");
                    }
                    found |= (x - realRoot.ToEntity()).Simplify() == 0;
                }
            Assert.True(found, $"{realRoot} is not among {solutions}");
        }
    }
}
