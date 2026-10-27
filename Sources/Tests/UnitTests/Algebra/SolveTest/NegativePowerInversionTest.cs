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
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// Inverting a negative whole power found no roots: the roots of unity it multiplies by were
    /// counted up to the exponent, and up to -2 is none. So an equation that reaches one inside
    /// another, as <c>1/(1 + x^(-2)) = 1/2</c> does, was answered with the empty set, a claim that
    /// it has no roots, where it has 1 and -1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2144">#2144</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class NegativePowerInversionTest
    {
        private static Set Solve(string statement, string x = "x") => statement.ToEntity().Solve(x);

        private static Entity IsIn(string value, Set set) => value.ToEntity().In(set).Simplify();

        [Theory]
        [InlineData("1/(1 + x^(-2)) = 1/2", "1", "-1", "2")]
        [InlineData("1/(2 + x^(-3)) = 1/3", "1", "-1/2 + 1/2 * sqrt(3) * i", "-1")]
        [InlineData("2/(1 + x^(-2)) = 1", "1", "-1", "0")]
        [InlineData("1/(3 + (x + 1)^(-2)) = 1/4", "0", "-2", "1")]
        public void EveryRootIsFound(string equation, string root, string otherRoot, string nonRoot)
        {
            var solved = Solve(equation);
            Assert.Equal(Entity.Boolean.True, IsIn(root, solved));
            Assert.Equal(Entity.Boolean.True, IsIn(otherRoot, solved));
            Assert.Equal(Entity.Boolean.False, IsIn(nonRoot, solved));
        }

        /// <summary>
        /// With a symbol on the right the answer is the two roots in terms of it, which at
        /// y = 1/2 are the roots above.
        /// </summary>
        [Fact]
        public void TheRootsInASymbolAreTheRootsAtEachValue()
        {
            var solved = Assert.IsType<FiniteSet>(Solve("1/(1 + x^(-2)) = y"));
            Assert.Equal(2, solved.Count);
            var atAHalf = solved.Select(root => root.Substitute("y", "1/2".ToEntity()).Simplify()).ToList();
            Assert.Contains(atAHalf, root => root == 1);
            Assert.Contains(atAHalf, root => root == -1);
        }

        /// <summary>A negative power is never zero, so this one does have none.</summary>
        [Fact]
        public void ANegativePowerIsNeverZero()
            => Assert.Equal(Set.Empty, Solve("1/(1 + x^(-2)) = 1"));
    }
}
