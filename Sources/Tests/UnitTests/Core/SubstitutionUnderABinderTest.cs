//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// Substituting under a binder: what is bound is not reached, a value holding the bound name is
    /// not captured, and a body the target does not occur in is not traversed again at every level.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1808">#1808</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class SubstitutionUnderABinderTest
    {
        private static Entity Nested(int depth, Entity upper)
        {
            if (depth == 0)
                return 1;
            var dummy = Variable.CreateVariableUnchecked($"u_{depth}");
            return new Integralf(Nested(depth - 1, dummy), dummy, (0, upper));
        }

        /// <summary>
        /// One integral per level, the target in the outermost bound only. Renaming every body three
        /// times at every level made this take about twice as long per level, past a few minutes by
        /// twenty; it is linear in the depth now. The answer is the chain built with the replacement.
        /// </summary>
        [Fact]
        public void AChainOfIntegralsIsSubstitutedInItsOuterBoundOnly()
        {
            var target = Variable.CreateVariableUnchecked("target");
            var replacement = Variable.CreateVariableUnchecked("w");
            Assert.Equal(Nested(20, replacement), Nested(20, target).Substitute(target, replacement));
        }

        [Fact]
        public void TheBoundNameIsNotReached()
        {
            var integral = new Integralf("t^2 + a".ToEntity(), "t", (0, "b".ToEntity()));
            Assert.Equal(integral, integral.Substitute("t", 5));
            Assert.Equal(integral, integral.Substitute("t^2".ToEntity(), 5));
            Assert.Equal(new Integralf("t^2 + 3".ToEntity(), "t", (0, "b".ToEntity())), integral.Substitute("a", 3));
        }

        [Fact]
        public void AValueHoldingTheBoundNameIsNotCaptured()
        {
            var integral = new Integralf("t + a".ToEntity(), "t", (0, 1));
            var substituted = (Integralf)integral.Substitute("a", "t");
            Assert.NotEqual(Variable.CreateVariableUnchecked("t"), substituted.Var);
            Assert.Equal(1.5.ToNumber(), substituted.Substitute("t", 1).EvalNumerical());
            var sum = (Summationf)new Summationf("k * a".ToEntity(), "k", 1, 3).Substitute("a", "k");
            Assert.NotEqual(Variable.CreateVariableUnchecked("k"), sum.Var);
            Assert.Equal(12.ToNumber(), sum.Substitute("k", 2).EvalNumerical());
        }

        [Fact]
        public void ASumIsSubstitutedOutsideItsIndex()
        {
            var sum = new Summationf("k * a".ToEntity(), "k", 1, "n".ToEntity());
            Assert.Equal(new Summationf("k * 2".ToEntity(), "k", 1, "n".ToEntity()), sum.Substitute("a", 2));
            Assert.Equal(new Summationf("k * a".ToEntity(), "k", 1, 4), sum.Substitute("n", 4));
            Assert.Equal(sum, sum.Substitute("k", 7));
        }
    }
}
