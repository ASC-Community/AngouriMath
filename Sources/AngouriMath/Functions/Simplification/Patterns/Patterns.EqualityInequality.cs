//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Boolean;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace AngouriMath.Functions
{
    internal static partial class Patterns
    {
        private static bool IsRealPositive(Entity entity)
            => entity is Real re && re > 0;

        private static bool IsRealNegative(Entity entity)
            => entity is Real re && re < 0;

        private static bool IsNonZero(Entity entity)
             => !IsZero(entity);

        private static bool IsZero(Entity entity)
            => entity is Real re && Real.IsZero(re);

        // Suggestions to refactor this?
        private static bool OppositeSigns(ComparisonSign left, ComparisonSign right)
        {
            if (left is Lessf)
                return right is Greaterf or GreaterOrEqualf or Equalsf;
            if (left is LessOrEqualf)
                return right is Greaterf;
            if (left is Greaterf)
                return right is Lessf or LessOrEqualf or Equalsf;
            if (left is GreaterOrEqualf)
                return right is Lessf;
            if (left is Equalsf)
                return right is Lessf or Greaterf;
            return false;
        }

        /// Two comparisons of one pair of operands that between them leave no case. Exactly one
        /// of a &lt; b, a = b and a &gt; b holds on an ordered field, so a disjunction covering
        /// all three is valid there -- and `&lt;` with `&gt;=` is how excluded middle for an
        /// order comparison is usually written.
        private static bool ExhaustiveSigns(ComparisonSign left, ComparisonSign right)
        {
            if (left is Lessf)
                return right is GreaterOrEqualf;
            if (left is LessOrEqualf)
                return right is Greaterf or GreaterOrEqualf;
            if (left is Greaterf)
                return right is LessOrEqualf;
            if (left is GreaterOrEqualf)
                return right is Lessf or LessOrEqualf;
            return false;
        }

        // Whether the ordering is there to be read at this operand. A node's declared codomain
        // is the only thing that can say so for a symbol: a Variable is Domain.Any until it is
        // told otherwise.
        private static bool IsKnownReal(Entity entity)
            => entity.Codomain is AngouriMath.Core.Domain.Prime
                              or AngouriMath.Core.Domain.PositiveInteger
                              or AngouriMath.Core.Domain.NonNegativeInteger
                              or AngouriMath.Core.Domain.Integer
                              or AngouriMath.Core.Domain.Rational
                              or AngouriMath.Core.Domain.Real;

        /// The condition under which an order comparison against <paramref name="entity"/> has
        /// a truth value. Over the complex plane it need not have one -- <c>i &lt; 0</c> is
        /// <c>NaN</c>, since the complex numbers are not ordered -- so a rule that decides a
        /// conjunction or a disjunction of comparisons has to carry the condition rather than
        /// help itself to it. <see cref="Entity.Provided(Entity)"/> drops the condition when it
        /// is <c>True</c>, so nothing is attached where the operands are already known real or
        /// where the reading is real-valued to begin with.
        /// https://github.com/asc-community/AngouriMath/issues/876
        private static Entity OrderedCondition(Entity entity)
            => MathS.Settings.Codomain.Value is AngouriMath.Core.Domain.Real || IsKnownReal(entity)
                ? True
                : entity.In(AngouriMath.Core.Domain.Real);

        /// The condition under which <paramref name="statement"/> has a truth value at all.
        /// Equality and set membership have one everywhere on the complex plane; an order
        /// comparison has one only where both of its operands are real.
        /// https://github.com/asc-community/AngouriMath/issues/876
        internal static Entity TruthCondition(Entity statement) => statement switch
        {
            Equalsf => True,
            ComparisonSign sign =>
                BothHold(OrderedCondition(sign.DirectChildren[0]), OrderedCondition(sign.DirectChildren[1])),
            _ => True
        };

        // `True & c` is an Andf and not True, which would stop Provided from dropping it.
        private static Entity BothHold(Entity left, Entity right)
            => left == True ? right : right == True ? left : left & right;

        /// <summary>
        /// The predicates the rules key on, named so that <c>MatchedRules.InequalityEquality</c>
        /// asks the same questions this <c>switch</c> asks rather than restating them. Where a
        /// condition is about a branch or an ordering, two copies of it is how the two come to
        /// disagree.
        /// </summary>
        internal static bool IsZeroReal(Entity entity) => IsZero(entity);

        /// <inheritdoc cref="IsZeroReal"/>
        internal static bool IsRealAbove(Entity entity, int bound) => bound == 0 && IsRealPositive(entity);

        /// <inheritdoc cref="IsZeroReal"/>
        internal static bool IsRealBelow(Entity entity, int bound) => bound == 0 && IsRealNegative(entity);

        /// <summary>Whether two comparisons are about the same pair of operands, in the same order.</summary>
        internal static bool SameOperands(Entity left, Entity right)
            => left is ComparisonSign && right is ComparisonSign
               && left.DirectChildren[0] == right.DirectChildren[0]
               && left.DirectChildren[1] == right.DirectChildren[1];

        /// <inheritdoc cref="SameOperands"/>
        internal static bool HaveOppositeSigns(Entity left, Entity right)
            => OppositeSigns((ComparisonSign)left, (ComparisonSign)right);

        /// <inheritdoc cref="SameOperands"/>
        internal static bool HaveExhaustiveSigns(Entity left, Entity right)
            => ExhaustiveSigns((ComparisonSign)left, (ComparisonSign)right);

        /// <summary>The <see cref="OrderedCondition"/> of one operand of a comparison, by index.</summary>
        internal static Entity OrderedConditionOf(Entity comparison, int operand)
            => OrderedCondition(comparison.DirectChildren[operand]);

        /// <inheritdoc cref="OrderedConditionOf"/>
        internal static Entity OrderedConditionFor(Entity entity) => OrderedCondition(entity);

        /// <summary>
        /// Whether pushing a <c>not</c> through this conjunction or disjunction leaves the
        /// expression no more complex: at most one operand may be something other than a
        /// comparison, since only a comparison absorbs the negation into itself.
        /// </summary>
        /// <remarks>
        /// <c>Notf(Equalsf)</c> has the same complexity as <c>Equalsf</c> in the cost model, so a
        /// negation counts as a comparison here.
        /// </remarks>
        internal static bool MayPushNotInside(IEnumerable<Entity> operands, bool insideConjunction)
            => operands.Count(one => one is not ComparisonSign and not Notf
                                     && (insideConjunction ? one is not Orf : one is not Andf)) <= 1;

        /// <summary>
        /// De Morgan: the negation of each operand, joined by the dual connective. Written once
        /// and asked by both the <c>switch</c> and <c>MatchedRules.InequalityEquality</c>, because
        /// it is a fold over a chain of any length rather than a shape, and a pattern says shapes.
        /// </summary>
        internal static Entity PushNotInside(IEnumerable<Entity> operands, bool disjoin)
            => operands
                .Select(one => InequalityEqualityRules(one switch { Notf(var inner) => inner, var whole => new Notf(whole) }))
                .Aggregate((left, right) => disjoin ? left | right : left & right);

        [AddressableRules]
        internal static Entity InequalityEqualityRules(Entity x) => x switch
        {
            // `a < b or a = b` is `a <= b`, and the four arms below it are the same law with the
            // comparison written the other way round -- so they answer the other way round too.
            // `b < a or a = b` is `a >= b`, not `a <= b`: at a = 1, b = 2 the disjunction is
            // False and `a <= b` is True. Four of these eight carried their neighbour's answer.
            //
            // They are only reachable with both operands symbolic. With a number on one side,
            // the `Lessf(var @const, ...)` arm further down rewrites `2 < x` to `x > 2` earlier
            // in the same pass, so the disjunction is always looked at with both halves written
            // the same way round and one of the four correct arms matches.
            // https://github.com/asc-community/AngouriMath/issues/1077
            Orf(Lessf(var any1, var any2), Equalsf(var any1a, var any2a)) when any1 == any1a && any2 == any2a => any1 <= any2,
            Orf(Lessf(var any2, var any1), Equalsf(var any1a, var any2a)) when any1 == any1a && any2 == any2a => any1 >= any2,
            Orf(Greaterf(var any1, var any2), Equalsf(var any1a, var any2a)) when any1 == any1a && any2 == any2a => any1 >= any2,
            Orf(Greaterf(var any2, var any1), Equalsf(var any1a, var any2a)) when any1 == any1a && any2 == any2a => any1 <= any2,
            Orf(Equalsf(var any1a, var any2a), Lessf(var any1, var any2)) when any1 == any1a && any2 == any2a => any1 <= any2,
            Orf(Equalsf(var any1a, var any2a), Lessf(var any2, var any1)) when any1 == any1a && any2 == any2a => any1 >= any2,
            Orf(Equalsf(var any1a, var any2a), Greaterf(var any1, var any2)) when any1 == any1a && any2 == any2a => any1 >= any2,
            Orf(Equalsf(var any1a, var any2a), Greaterf(var any2, var any1)) when any1 == any1a && any2 == any2a => any1 <= any2,

            Notf(Greaterf(var any1, var any2)) => any1 <= any2,
            Notf(Lessf(var any1, var any2)) => any1 >= any2,
            Notf(GreaterOrEqualf(var any1, var any2)) => any1 < any2,
            Notf(LessOrEqualf(var any1, var any2)) => any1 > any2,
            // If we have a bunch of comparison operators combined with AND/OR and NOT outside, we can push the NOT inside and flip all the operators.
            // For complexity to not increase, maximum one AND/OR component can be something other than a comparison operator to propagate NOT into.
            // e.g. not (a > b and b = c) becomes (a <= b or not b = c)
            // Note that Notf(Equalsf) has the same complexity as Equalsf in ComplexityCriteria, so it can be treated as a comparison operator here.
            Notf(Andf a) when MayPushNotInside(Andf.LinearChildren(a), insideConjunction: true)
                => PushNotInside(Andf.LinearChildren(a), disjoin: true),
            Notf(Orf a) when MayPushNotInside(Orf.LinearChildren(a), insideConjunction: false)
                => PushNotInside(Orf.LinearChildren(a), disjoin: false),

            Impliesf(Andf(Greaterf(var any1, var any2), Greaterf(var any2a, var any3)), Greaterf(var any1a, var any3a))
                when any1 == any1a && any2 == any2a && any3 == any3a => True.Provided(any1.DomainCondition).Provided(any2.DomainCondition).Provided(any3.DomainCondition),

            Impliesf(Andf(Lessf(var any1, var any2), Lessf(var any2a, var any3)), Lessf(var any1a, var any3a))
                when any1 == any1a && any2 == any2a && any3 == any3a => True.Provided(any1.DomainCondition).Provided(any2.DomainCondition).Provided(any3.DomainCondition),

            Equalsf(var zero, var anyButZero) when IsZero(zero) && !IsZero(anyButZero) => anyButZero.EqualTo(zero),
            Greaterf(var zero, var anyButZero) when IsZero(zero) && !IsZero(anyButZero) => anyButZero < zero,
            Lessf(var zero, var anyButZero) when IsZero(zero) && !IsZero(anyButZero) => anyButZero > zero,
            GreaterOrEqualf(var zero, var anyButZero) when IsZero(zero) && !IsZero(anyButZero) => anyButZero <= zero,
            LessOrEqualf(var zero, var anyButZero) when IsZero(zero) && !IsZero(anyButZero) => anyButZero >= zero,

            Equalsf(var @const, var anyButConst) when @const is Number && anyButConst is not Number => anyButConst.EqualTo(@const),
            Greaterf(var @const, var anyButConst) when @const is Number && anyButConst is not Number => anyButConst < @const,
            Lessf(var @const, var anyButConst) when @const is Number && anyButConst is not Number => anyButConst > @const,
            GreaterOrEqualf(var @const, var anyButConst) when @const is Number && anyButConst is not Number => anyButConst <= @const,
            LessOrEqualf(var @const, var anyButConst) when @const is Number && anyButConst is not Number => anyButConst >= @const,

            Andf(ComparisonSign left, ComparisonSign right) when
            left.DirectChildren[0] == right.DirectChildren[0] &&
            left.DirectChildren[1] == right.DirectChildren[1] &&
            OppositeSigns(left, right) =>
                False.Provided(OrderedCondition(left.DirectChildren[0]))
                     .Provided(OrderedCondition(left.DirectChildren[1])),

            // The other half of the same law. The unsatisfiable conjunction above was decided
            // and the valid disjunction was not, so the library was taking the half of excluded
            // middle that needs the operands to be real and skipping the half that needs the
            // same thing. https://github.com/asc-community/AngouriMath/issues/876
            Orf(ComparisonSign left, ComparisonSign right) when
            left.DirectChildren[0] == right.DirectChildren[0] &&
            left.DirectChildren[1] == right.DirectChildren[1] &&
            ExhaustiveSigns(left, right) =>
                True.Provided(OrderedCondition(left.DirectChildren[0]))
                    .Provided(OrderedCondition(left.DirectChildren[1])),

            Equalsf(Powf(var any1, var rePo), var zero) when IsRealPositive(rePo) && IsZero(zero) => any1.EqualTo(zero),
            Equalsf(Divf(Integer(1), var expr), var zero) when IsZero(zero) => new Providedf(false, !expr.EqualTo(0)),

            // The following set of patterns might be simplified

            // 4 * a ? 0
            Equalsf        (Mulf(var rePo, var any1), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Mulf(var rePo, var any1), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            GreaterOrEqualf(Mulf(var rePo, var any1), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,
            Lessf          (Mulf(var rePo, var any1), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            LessOrEqualf   (Mulf(var rePo, var any1), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,

            // a * 4 ? 0
            Equalsf        (Mulf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Mulf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            GreaterOrEqualf(Mulf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,
            Lessf          (Mulf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            LessOrEqualf   (Mulf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,

            // -4 * a ? 0
            Equalsf        (Mulf(var rePo, var any1), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Mulf(var rePo, var any1), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            GreaterOrEqualf(Mulf(var rePo, var any1), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,
            Lessf          (Mulf(var rePo, var any1), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            LessOrEqualf   (Mulf(var rePo, var any1), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,

            // a * -4 ? 0
            Equalsf        (Mulf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Mulf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            GreaterOrEqualf(Mulf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,
            Lessf          (Mulf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            LessOrEqualf   (Mulf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,

            // a / 4 ? 0
            Equalsf        (Divf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Divf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            GreaterOrEqualf(Divf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,
            Lessf          (Divf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            LessOrEqualf   (Divf(var any1, var rePo), var zeroEnt) when IsRealPositive(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,

            // a / -4 ? 0
            Equalsf        (Divf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1.EqualTo(Integer.Zero),
            Greaterf       (Divf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 < Integer.Zero,
            GreaterOrEqualf(Divf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 <= Integer.Zero,
            Lessf          (Divf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 > Integer.Zero,
            LessOrEqualf   (Divf(var any1, var rePo), var zeroEnt) when IsRealNegative(rePo) && IsZero(zeroEnt) => any1 >= Integer.Zero,

            // a! = 0
            // A factorial is never zero *where it is defined*, and at a negative integer it is
            // not: `(-1)!` is a pole, so `(-1)! = 0` is NaN rather than False. The condition owed
            // is therefore the factorial's own -- `x in ZZ implies x >= 0`.
            //
            // What was written was `Factorialf({ DomainCondition: var condition })`, a property
            // pattern on the factorial's *argument*. For a bare variable that condition is True,
            // and `Provided` drops a True, so `x! = 0` went out as an unconditioned False and
            // answered a question the original declines.
            // https://github.com/asc-community/AngouriMath/issues/1081
            Equalsf(Factorialf factorial, var zeroEnt) when IsZero(zeroEnt)
                => False.Provided(factorial.DomainCondition),

            // The DomainCondition is about singularities and says nothing about where the
            // ordering is defined, so both conditions are needed: `x < x` is False on the real
            // line and NaN at x = i.
            Greaterf(var any1, var any1a) when any1 == any1a => False.Provided(any1.DomainCondition).Provided(OrderedCondition(any1)),
            Lessf(var any1, var any1a) when any1 == any1a => False.Provided(any1.DomainCondition).Provided(OrderedCondition(any1)),
            GreaterOrEqualf(var any1, var any1a) when any1 == any1a => True.Provided(any1.DomainCondition).Provided(OrderedCondition(any1)),
            LessOrEqualf(var any1, var any1a) when any1 == any1a => True.Provided(any1.DomainCondition).Provided(OrderedCondition(any1)),

            _ => x
        };
    }
}
