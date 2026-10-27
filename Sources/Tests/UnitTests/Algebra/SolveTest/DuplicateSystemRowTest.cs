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
    /// Two branches of an elimination can reach one solution, and each was answered: two circles
    /// that meet at two points gave three rows, one of them the other again, and where the
    /// second branch went through Newton's method, again as a decimal.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2138">#2138</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class DuplicateSystemRowTest
    {
        private static Matrix Solve(string first, string second)
        {
            var solved = MathS.Equations(first.ToEntity(), second.ToEntity()).Solve("x", "y");
            Assert.NotNull(solved);
            return solved!;
        }

        private static bool HasRow(Matrix solutions, string x, string y)
            => Enumerable.Range(0, solutions.RowCount).Any(row =>
                (solutions[row, 0] - x).Simplify() == 0 && (solutions[row, 1] - y).Simplify() == 0);

        [Theory]
        // the copy was a decimal: x = 1.4999999999999997
        [InlineData("(x - 1/2)^2 + (y - sqrt(3)/2)^2 - 1", "0", "0", "3/2", "sqrt(3)/2")]
        // the copy was the same row again
        [InlineData("(x - 3/2)^2 + (y - sqrt(3)/2)^2 - 1", "1/2", "sqrt(3)/2", "2", "0")]
        public void TwoCirclesMeetingTwiceGiveTwoExactRows(string circle, string x1, string y1, string x2, string y2)
        {
            var solutions = Solve(circle, "(x - 1)^2 + y^2 - 1");
            Assert.Equal(2, solutions.RowCount);
            Assert.True(HasRow(solutions, x1, y1), $"({x1}, {y1}) is not among {solutions}");
            Assert.True(HasRow(solutions, x2, y2), $"({x2}, {y2}) is not among {solutions}");
            Assert.DoesNotContain(solutions.Nodes, node => node is Number { IsExact: false });
        }
    }
}
