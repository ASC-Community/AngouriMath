//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra.AnalyticalSolving;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A polynomial of degree one or more has a root over the complex numbers, so where every
    /// candidate a solver found for one fails its check, the solver failed, and the empty set
    /// would claim there is no root. <c>(x + 1)^3 = 2</c> and
    /// <c>31x^4 - 62x^3 + 33x^2 - 2x - 1 = 0</c> were answered <c>{ }</c> that way. Asked here of
    /// the check itself, with candidates that fail, since no polynomial reaches it so any more.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class APolynomialHasARootTest
    {
        [Theory]
        [InlineData("x^2 - 2")]
        [InlineData("(x + 1)^3 - 2")]
        [InlineData("31x^4 - 62x^3 + 33x^2 - 2x - 1")]
        public void APolynomialWhoseCandidatesAllFailIsLeftUnsolved(string polynomial)
            => Assert.IsType<ConditionalSet>(
                StatementSolver.WithoutSpuriousRoots(new FiniteSet((Entity)5, (Entity)7), polynomial.ToEntity(), "x"));

        /// <summary>The roots that hold are kept, as before.</summary>
        [Fact]
        public void TheRootsThatHoldAreKept()
            => Assert.Equal("{ 2 }".ToEntity(),
                StatementSolver.WithoutSpuriousRoots(new FiniteSet((Entity)2, (Entity)5), "x^2 - 4".ToEntity(), "x").InnerSimplified);

        /// <summary>
        /// An equation that is not a polynomial can have no root, and its empty set is an answer:
        /// <c>sqrt(x) = -1</c>. So can a polynomial over the reals: <c>x^2 + 1 = 0</c>.
        /// </summary>
        [Fact]
        public void TheOthersCanHaveNoRoot()
        {
            Assert.Equal(Empty, StatementSolver.WithoutSpuriousRoots(new FiniteSet((Entity)1), "sqrt(x) + 1".ToEntity(), "x"));
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(Empty, StatementSolver.WithoutSpuriousRoots(new FiniteSet((Entity)5), "x^2 + 1".ToEntity(), "x"));
        }
    }
}
