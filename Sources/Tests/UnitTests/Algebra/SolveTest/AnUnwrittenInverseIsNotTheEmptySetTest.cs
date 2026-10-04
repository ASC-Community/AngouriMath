//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// An equation in a function whose preimage the inverter cannot write -- a factorial, a
    /// binomial coefficient, a gcd -- was answered with the empty set, a claim that
    /// it has no roots. <c>x! = 6</c> has the root 3. Such an equation is now left unsolved, as
    /// the set of <c>x</c> for which it holds.
    /// </summary>
    /// <remarks>
    /// These assert what the answers mean -- which values are in the set -- rather than the
    /// shape they are written in, so that solving one of them properly later does not fail them.
    /// A solution written as a family in a whole parameter, as a remainder's is, is checked at
    /// values of the parameter instead, since membership does not range over it.
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class AnUnwrittenInverseIsNotTheEmptySetTest
    {
        private static Set Solve(string statement) => statement.ToEntity().Solve("x");

        private static Entity IsIn(string value, Set set) => value.ToEntity().In(set).Simplify();

        [Theory]
        [InlineData("x! = 6", "3", "2")]
        [InlineData("binomial(x, 2) = 3", "-2", "2")]
        [InlineData("gcd(x, 4) = 2", "6", "8")]
        [InlineData("max(x, 1) = 3", "3", "1")]
        [InlineData("prime(x) = 7", "4", "3")]
        [InlineData("phi(x) = 4", "5", "7")]
        public void ARootIsInTheAnswerAndANonRootIsNot(string equation, string root, string other)
        {
            var solved = Solve(equation);
            Assert.NotEqual(Set.Empty, solved);
            Assert.Equal(Entity.Boolean.True, IsIn(root, solved));
            Assert.Equal(Entity.Boolean.False, IsIn(other, solved));
        }

        /// <summary>
        /// A set, a cardinality or a power set of the unknown has a set of sets for its preimage,
        /// which the inverter cannot write, and it threw <c>NotSufficientlySupportedException</c>
        /// out of <c>Solve</c> where its contract is to decline and leave the equation unsolved.
        /// https://github.com/asc-community/AngouriMath/issues/1544
        /// </summary>
        [Theory]
        [InlineData("card(x) = 3", "{1, 2, 3}", "{1}")]
        [InlineData("(x /\\ {1, 2}) = {1}", "{1}", "{2}")]
        [InlineData("(x \\ {1}) = {2}", "{2}", "{1, 2, 3}")]
        [InlineData("powerset(x) = {{}, {1}}", "{1}", "{2}")]
        public void ASetOfSetsIsLeftUnsolved(string equation, string root, string other)
            => ARootIsInTheAnswerAndANonRootIsNot(equation, root, other);

        /// <summary>
        /// The factorial is the gamma function one along, which has no zeros, so a factorial
        /// equal to zero does have none, and the roots found beside one are the whole answer.
        /// </summary>
        [Fact]
        public void AFactorialIsNeverZero()
        {
            Assert.Equal(Set.Empty, Solve("x! = 0"));
            Assert.Equal("{ 1 }".ToEntity(), Solve("(x - 1) x! = 0"));
        }

        /// <summary>A value outside arcsin's range is still one it never takes.</summary>
        [Fact]
        public void AnInverseThatIsWrittenStillAnswersNone()
            => Assert.Equal(Set.Empty, Solve("arcsin(x) = 5"));

        /// <summary>
        /// A condition beside an unwritten inverse is kept: 3 is a root of <c>x! = 6</c> and is
        /// excluded by <c>x &gt; 7</c>. That nothing else satisfies both is true, but the solver
        /// has not shown it, so it does not answer the empty set.
        /// </summary>
        [Fact]
        public void AConditionBesideItIsKept()
        {
            var solved = Solve("(x! provided x > 7) = 6");
            Assert.IsType<ConditionalSet>(solved);
            Assert.Equal(Entity.Boolean.False, IsIn("3", solved));
        }
    }
}
