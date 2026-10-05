//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using System;
using static AngouriMath.Entity.Set;

namespace AngouriMath
{
    partial record Entity
    {
        public partial record Variable
        {
            private protected override Entity IntrinsicCondition => true;
            /// <inheritdoc/>
            /// <remarks>
            /// A variable is itself, whatever it is called. What a name is worth is asked of the
            /// node and not of a table keyed by name, which is what used to make a bound `e`
            /// carry Euler's number.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/984">#984</a>
            /// </remarks>
            protected override Entity InnerSimplify(bool isExact) => this;
        }

        public partial record Constant
        {
            /// <inheritdoc/>
            /// <remarks>Exactly, a constant is itself; approximately, it is its value.</remarks>
            protected override Entity InnerSimplify(bool isExact) => isExact ? this : Value;
        }

        /// <summary>
        /// Whether <paramref name="value"/>, the exact number <paramref name="node"/> evaluates
        /// to, is a zero that rounding alone made: <paramref name="node"/> is a product, a quotient
        /// or a power of numbers none of which is zero, or a sum of numbers of one sign, and is
        /// not zero itself.
        /// </summary>
        /// <remarks>
        /// Evaluation rounds a value within <see cref="MathS.Settings.DowncastingTolerance"/> of an
        /// integer onto it, ten to the minus fifty at the default hundred digits, which is what
        /// makes the residual of a cancellation the zero it is. A product cancels nothing, and was
        /// rounded the same way: <c>1/pi^136</c>, about <c>2.4e-68</c>, evaluated to <c>0</c>, and
        /// that zero, being an exact number, was taken as its value. The common denominator
        /// raises constants to such powers, and <c>Simplify</c> returned <c>0</c> for
        /// <c>x^n ((1 - d^2)/x - x)^3 (1 + x^2 - d x)/(x - d)/pi^2</c>. A sum whose terms have
        /// one sign cancels nothing either; any other sum's zero may be a cancellation's, and is
        /// taken as it was.
        /// https://github.com/asc-community/AngouriMath/issues/1769
        /// </remarks>
        private static bool IsAZeroOnlyRoundingMade(Entity node, Entity value)
            => value is Integer { IsZero: true }
               && (node is Mulf or Divf or Powf && CannotBeZero(node) || node is Sumf or Minusf && SignOfANonzero(node) is not null);

        /// <summary>
        /// Whether <paramref name="expr"/> is a number or a constant that is not zero, or a product,
        /// a quotient or a power of those with a finite divisor or exponent.
        /// </summary>
        private static bool CannotBeZero(Entity expr) => expr switch
        {
            Complex number => !number.IsZero && number.IsFinite,
            Variable { IsConstant: true } constant => constant.Evaled is Complex { IsZero: false, IsFinite: true },
            Mulf(var left, var right) => CannotBeZero(left) && CannotBeZero(right),
            Divf(var dividend, var divisor) => CannotBeZero(dividend) && divisor.Evaled is Complex { IsFinite: true },
            Powf(var @base, var exponent) => CannotBeZero(@base) && exponent.Evaled is Complex { IsFinite: true },
            _ => SignOfANonzero(expr) is not null
        };

        /// <summary>
        /// The sign of <paramref name="expr"/> where it is a real number or a constant that is not
        /// zero, or a product, a quotient, a power or a sum of those whose sign the parts decide;
        /// <see langword="null"/> where they do not.
        /// </summary>
        private static int? SignOfANonzero(Entity expr) => expr switch
        {
            Real number => !number.IsZero && number.IsFinite ? (number.IsPositive ? 1 : -1) : null,
            Variable { IsConstant: true } constant => constant.Evaled is Real { IsZero: false } value ? (value.IsPositive ? 1 : -1) : null,
            Mulf(var left, var right) => SignOfANonzero(left) * SignOfANonzero(right),
            Divf(var dividend, var divisor) => SignOfANonzero(dividend) * SignOfANonzero(divisor),
            Powf(var @base, Integer power) => SignOfANonzero(@base) is { } sign ? (power.EInteger.IsEven ? 1 : sign) : null,
            Powf(var @base, var exponent) => SignOfANonzero(@base) == 1 && exponent.Evaled is Real { IsFinite: true } ? 1 : null,
            Sumf(var left, var right) => SignOfANonzero(left) is { } sign && SignOfANonzero(right) == sign ? sign : null,
            Minusf(var left, var right) => SignOfANonzero(left) is { } sign && SignOfANonzero(right) == -sign ? sign : null,
            _ => null
        };

        /// <summary>
        /// For two-argument nodes
        /// Used in InnerSimplify and InnerEval
        /// Allows to avoid looking over all the combinations with piecewise, tensor, finiteset
        /// </summary>
        /// <param name="left">
        /// Left argument
        /// </param>
        /// <param name="right">
        /// Right argument
        /// </param>
        /// <param name="operation">
        /// That is the main switch for the types. It must return null if no suitable couple of types is found,
        /// so that the method could move on to the matrix choice
        /// </param>
        /// <param name="defaultCtor">
        /// If no suitable case in switch found, it should return the default node, for example, for sum it would be
        /// <code>(a, b) => a + b</code>
        /// </param>
        /// <param name="isExact">
        /// Check if the number is exact and, if so, return it.
        /// </param>
        /// <param name="propagateSet">
        /// Set operations should not be applied on all pairs of elements when it cannot be simplified.
        /// </param>
        /// <param name="settlesNaN">
        /// Whether <paramref name="operation"/> is asked about a <c>NaN</c> operand instead of the
        /// result being <c>NaN</c> outright. A logical connective can settle one -- <c>false and u</c>
        /// is <c>false</c> whatever <c>u</c> is -- and arithmetic cannot, so this is off by default:
        /// <c>NaN * 0</c> must not become <c>0</c> just because a rule for a zero factor exists.
        /// https://github.com/asc-community/AngouriMath/issues/880
        /// </param>
        private Entity ExpandOnTwoArguments(
            Entity left,
            Entity right,
            Func<Entity, Entity, Entity?> operation,
            Func<Entity, Entity, Entity, Entity> defaultCtor,
            bool isExact,
            bool propagateSet = true,
            bool settlesNaN = false)
        {
            if (isExact && this.Evaled is (Number { IsExact: true } or Boolean) and var n && !IsAZeroOnlyRoundingMade(this, n))
                return n;
            left = left.InnerSimplified(isExact);
            right = right.InnerSimplified(isExact);
            if (left.IsNaN || right.IsNaN)
            {
                // A connective gets first refusal on an undefined operand, and hands back null where
                // it cannot settle the case, which is what falls through to NaN here. Its own table
                // is already the three-valued one: `and` reads (_, false) as false and (true, _) as
                // its right operand, so a NaN that genuinely decides nothing stays NaN by arriving
                // back out of the switch.
                if (settlesNaN && operation(left, right) is { } settled)
                    return settled;
                return MathS.NaN;
            }

            if (operation(left, right) is { } preRes)
                return preRes;

            Entity ops(Entity a, Entity b)
            {
                if (operation(a, b) is { } res)
                    return res;
                if (isExact && defaultCtor(this, a, b) is var built && built.Evaled is Number { IsExact: true } n && !IsAZeroOnlyRoundingMade(built, n))
                    return n;
                return defaultCtor(this, a, b);
            }

            return (left, right) switch
            {
                (Providedf a, Providedf b) => ops(a.Expression, b.Expression).Provided(a.Predicate & b.Predicate),
                (Providedf a, var b) => ExpandOnTwoArguments(a.Expression, b, operation, defaultCtor, isExact).Provided(a.Predicate),
                (var a, Providedf b) => ExpandOnTwoArguments(a, b.Expression, operation, defaultCtor, isExact).Provided(b.Predicate),
                (Piecewise a, Piecewise b) => CombinedCaseByCase(a, b, operation, defaultCtor, isExact),
                (Piecewise a, var b) => a.ApplyToValues(a => ops(a, b)),
                (var a, Piecewise b) => b.ApplyToValues(b => ops(a, b)),
                (Matrix a, Matrix b) => a.InnerMatrix.Shape == b.InnerMatrix.Shape ? a.Elementwise(b, ops) : defaultCtor(this, left, right),
                (Matrix a, var b) => a.Elementwise(a => ops(a, b)),
                (var a, Matrix b) => b.Elementwise(b => ops(a, b)),
                _ => propagateSet ? (left, right) switch
                {
                    (FiniteSet a, FiniteSet b) => new FiniteSet((a, b).EachForEach().Select(s => ops(s.left, s.right))),
                    (FiniteSet a, var b) => a.Apply(a => ops(a, b)),
                    (var a, FiniteSet b) => b.Apply(b => ops(a, b)),
                    _ => defaultCtor(this, left, right)
                } : defaultCtor(this, left, right)
            };
        }

        /// <summary>
        /// A piecewise combined with a piecewise, case by case: every case of one against every
        /// case of the other, under the conjunction of their predicates.
        /// </summary>
        /// <remarks>
        /// A pair whose conjunction is <c>False</c> is not a case: it is dropped here, before
        /// its expression is even combined, rather than carried as <c>... provided False</c>
        /// until the piecewise is next simplified. Two piecewises that split on the same three
        /// signs of one quantity then combine to five cases and not nine -- the three that
        /// agree, and the two opposite strict signs, which are NaN rather than False off the
        /// real line and so stay -- and a sum of such piecewises stays at five instead of
        /// having 3^n cases after n additions.
        /// https://github.com/asc-community/AngouriMath/issues/1414
        /// </remarks>
        private Entity CombinedCaseByCase(Piecewise a, Piecewise b,
            Func<Entity, Entity, Entity?> operation, Func<Entity, Entity, Entity, Entity> defaultCtor, bool isExact)
        {
            // Every pair of cases, in order, and its conjunction; the expressions are combined only
            // for the pairs that are kept.
            var pairs = new List<(Providedf First, Providedf Second, Entity Predicate, Entity? Undecided)>();
            foreach (var (c1, c2) in (a.Cases, b.Cases).EachForEach())
            {
                var predicate = (c1.Predicate & c2.Predicate).InnerSimplified;
                if (predicate == Boolean.False)
                    continue;
                switch (Contradiction(predicate))
                {
                    case (true, _):
                        continue;
                    case (false, var quantity):
                        pairs.Add((c1, c2, predicate, quantity));
                        break;
                }
            }
            // A pair that asks a quantity to be both positive and negative is false for a real
            // one and undecided off the real line, where it would stop the cases after it from
            // being reached. It goes only where every later pair tests that quantity too: those
            // are each false or undecided off the real line, never true, so nothing it stopped
            // is reached without it. A sum of piecewises split on the sign of `d/f` -- the
            // integrator's answer for `x sqrt(1 + d x) sqrt(1 + f x)` -- otherwise doubled its
            // cases with every term: 32,769 for six terms of `t^k/(d - f t^2)^4`.
            // https://github.com/asc-community/AngouriMath/issues/718
            var keep = new bool[pairs.Count];
            for (var i = pairs.Count - 1; i >= 0; i--)
                keep[i] = pairs[i].Undecided is not { } quantity
                    || !Enumerable.Range(i + 1, pairs.Count - i - 1).All(later => !keep[later] || TestsTheSignOf(pairs[later].Predicate, quantity));
            var cases = new List<Providedf>();
            for (var i = 0; i < pairs.Count; i++)
                if (keep[i])
                    cases.Add((ExpandOnTwoArguments(pairs[i].First.Expression, pairs[i].Second.Expression, operation, defaultCtor, isExact), pairs[i].Predicate).ToProvided());
            return MathS.Piecewise(cases);
        }

        /// <summary>
        /// Whether <paramref name="predicate"/>, a conjunction, asks one quantity <c>q</c> to be
        /// zero and not zero, or zero and of a sign -- false for every value, complex or not -- in
        /// <c>Always</c>; and otherwise the quantity it asks to be both positive and negative,
        /// which is false for a real one and undecided off the real line, or null. A quantity is
        /// read up to a constant factor, which only the direction of a sign depends on:
        /// <c>f = 0</c> and <c>not 2 f = 0</c> contradict each other.
        /// </summary>
        private static (bool Always, Entity? Undecided) Contradiction(Entity predicate)
        {
            var tests = new List<(Entity Quantity, int Relation)>();
            var choices = new List<Entity>();
            foreach (var conjunct in Conjuncts(predicate))
                if (SignTest(conjunct) is { } test)
                    tests.Add(test);
                else if (conjunct is Orf)
                    choices.Add(conjunct);
            Entity? undecided = null;
            for (var i = 0; i < tests.Count; i++)
                for (var j = i + 1; j < tests.Count; j++)
                    switch (Against(tests[i], tests[j]))
                    {
                        case (true, _):
                            return (true, null);
                        case (false, { } quantity):
                            undecided = quantity;
                            break;
                    }
            // A disjunction beside them contradicts them where each of its arms does: the arm a
            // quadratic off the real line owes, `not D in RR or D > 0`, against `D = 0` is false
            // for every value, and against `D < 0` false for a real one and NaN off the real
            // line. Read as neither, the sum of the antiderivatives over three powers of one
            // quadratic kept every pairing of their arms, 45 for
            // `2 d^3 t^2/((t^2 - c - i d)^3 (t^2 - c + i d))` where 9 hold every case.
            // https://github.com/asc-community/AngouriMath/issues/1788
            foreach (var choice in choices)
            {
                var (always, quantity, read) = (true, (Entity?)null, true);
                foreach (var arm in Disjuncts(choice))
                {
                    if (SignTest(arm) is not { } armTest)
                    {
                        read = false;
                        break;
                    }
                    var (armAlways, armQuantity) = (false, (Entity?)null);
                    foreach (var test in tests)
                        switch (Against(armTest, test))
                        {
                            case (true, _):
                                armAlways = true;
                                break;
                            case (false, { } q):
                                armQuantity ??= q;
                                break;
                        }
                    if (armAlways)
                        continue;
                    if (armQuantity is null || quantity is not null && quantity != armQuantity)
                    {
                        read = false;
                        break;
                    }
                    (always, quantity) = (false, armQuantity);
                }
                if (!read)
                    continue;
                if (always)
                    return (true, null);
                undecided = quantity;
            }
            return (false, undecided);
        }

        /// <summary>
        /// Two tests of one quantity <c>q</c>: in <c>Always</c> where no value satisfies both,
        /// and otherwise the quantity where a real one satisfies neither together and one off the
        /// real line makes a sign NaN; null where they do not test one quantity or agree.
        /// </summary>
        private static (bool Always, Entity? Undecided) Against((Entity Quantity, int Relation) first, (Entity Quantity, int Relation) second)
        {
            if (first.Quantity != second.Quantity)
                return (false, null);
            var (r1, r2) = (first.Relation, second.Relation);
            // 0 is "= 0", 2 is "not = 0", 1 and -1 the signs, 3 "not in RR": zero is real.
            if (r1 == 0 && r2 != 0 || r2 == 0 && r1 != 0)
                return (true, null);
            if (r1 * r2 == -1 || r1 == 3 && r2 is 1 or -1 || r2 == 3 && r1 is 1 or -1)
                return (false, first.Quantity);
            return (false, null);
        }

        private static IEnumerable<Entity> Disjuncts(Entity condition)
        {
            if (condition is Orf(var left, var right))
            {
                foreach (var disjunct in Disjuncts(left))
                    yield return disjunct;
                foreach (var disjunct in Disjuncts(right))
                    yield return disjunct;
            }
            else
                yield return condition;
        }

        /// <summary>Whether <paramref name="predicate"/> has a conjunct testing <paramref name="quantity"/> against zero.</summary>
        private static bool TestsTheSignOf(Entity predicate, Entity quantity)
            => Conjuncts(predicate).Any(conjunct => SignTest(conjunct) is var (q, relation) && q == quantity && relation is 0 or 1 or -1);

        /// <summary>
        /// A comparison of a quantity with zero as the quantity, with any constant factor taken
        /// out, and the relation: 0 for <c>= 0</c>, 2 for <c>not = 0</c>, and the sign for
        /// <c>&gt; 0</c> and <c>&lt; 0</c>, turned by a negative factor. Null for anything else.
        /// </summary>
        private static (Entity Quantity, int Relation)? SignTest(Entity conjunct)
        {
            (Entity, int)? read = conjunct switch
            {
                Equalsf(var q, var zero) when zero == Integer.Zero => (q, 0),
                Equalsf(var zero, var q) when zero == Integer.Zero => (q, 0),
                Notf(Equalsf(var q, var zero)) when zero == Integer.Zero => (q, 2),
                Notf(Equalsf(var zero, var q)) when zero == Integer.Zero => (q, 2),
                Greaterf(var q, var zero) when zero == Integer.Zero => (q, 1),
                Lessf(var zero, var q) when zero == Integer.Zero => (q, 1),
                Lessf(var q, var zero) when zero == Integer.Zero => (q, -1),
                Greaterf(var zero, var q) when zero == Integer.Zero => (q, -1),
                Notf(Inf(var q, SpecialSet.Reals)) => (q, 3),
                _ => null,
            };
            if (read is not var (quantity, relation))
                return null;
            // The constant factor out: `4 f (-d)` is `f (-d)`, and `2 f` is `f`. A real one, which
            // leaves `not in RR` as it is.
            if (quantity is Mulf(Real factor, var rest) && !factor.IsZero)
                (quantity, relation) = (rest, relation is 1 or -1 && factor.IsNegative ? -relation : relation);
            return (quantity, relation);
        }

        private Entity ExpandOnOneArgument(Entity expr, Func<Entity, Entity?> operation, Func<Entity, Entity, Entity> defaultCtor, bool isExact,
            bool propagateSet = true)
        {
            if (isExact && this.Evaled is (Number { IsExact: true } or Boolean) and var n)
                return n;

            expr = expr.InnerSimplified(isExact);
            if (operation(expr) is { } notNull)
                return notNull;

            Entity ops(Entity a)
            {
                if (operation(a) is { } res)
                    return res;
                if (isExact && defaultCtor(this, a).Evaled is Number { IsExact: true } n)
                    return n;
                return defaultCtor(this, a);
            }

            return expr switch
            {
                Providedf p => ExpandOnOneArgument(p.Expression, operation, defaultCtor, isExact).Provided(p.Predicate),
                Piecewise p => p.ApplyToValues(ops),
                Matrix t => t.Elementwise(ops),
                _ => propagateSet ? expr switch
                {
                    FiniteSet s => s.Apply(ops),
                    _ => defaultCtor(this, expr)
                } : defaultCtor(this, expr)
            };
        }

        private Entity ExpandOnTwoAndTArguments<T>(Entity left, Entity right, T third, Func<Entity, Entity, T, Entity?> operation, Func<Entity, Entity, Entity, T, Entity> defaultCtor, bool isExact,
            bool propagateSet = true)
        {
            if (isExact && this.Evaled is (Number { IsExact: true } or Boolean) and var n)
                return n;

            left = left.InnerSimplified(isExact);
            right = right.InnerSimplified(isExact);
            if (operation(left, right, third) is { } preRes)
                return preRes;

            Entity ops(Entity a, Entity b)
            {
                if (operation(a, b, third) is { } res)
                    return res;
                if (isExact && defaultCtor(this, a, b, third).Evaled is Number { IsExact: true } n)
                    return n;
                return defaultCtor(this, a, b, third);
            }

            return (left, right, third) switch
            {
                (Providedf a, Providedf b, _) => ops(a.Expression, b.Expression).Provided(a.Predicate & b.Predicate),
                (Providedf a, var b, _) => ExpandOnTwoAndTArguments(a.Expression, b, third, operation, defaultCtor, isExact).Provided(a.Predicate),
                (var a, Providedf b, _) => ExpandOnTwoAndTArguments(a, b.Expression, third, operation, defaultCtor, isExact).Provided(b.Predicate),
                (Piecewise a, Piecewise b, _) =>
                    MathS.Piecewise(

                        (a.Cases, b.Cases).EachForEach((c1, c2) =>
                        (
                        ExpandOnTwoAndTArguments(c1.Expression, c2.Expression, third, operation, defaultCtor, isExact)
                        , (c1.Predicate & c2.Predicate).InnerSimplified).ToProvided()
                        )

                        ),
                (Piecewise a, var b, _) => a.ApplyToValues(a => ops(a, b)),
                (var a, Piecewise b, _) => b.ApplyToValues(b => ops(a, b)),
                (Matrix a, Matrix b, _) => a.InnerMatrix.Shape == b.InnerMatrix.Shape ? a.Elementwise(b, ops) : defaultCtor(this, left, right, third),
                (Matrix a, var b, _) => a.Elementwise(a => ops(a, b)),
                (var a, Matrix b, _) => b.Elementwise(b => ops(a, b)),
                _ => propagateSet ? (left, right, third) switch
                {
                    (FiniteSet a, FiniteSet b, _) => new FiniteSet((a, b).EachForEach().Select(s => ops(s.left, s.right))),
                    (FiniteSet a, var b, _) => a.Apply(a => ops(a, b)),
                    (var a, FiniteSet b, _) => b.Apply(b => ops(a, b)),
                    _ => defaultCtor(this, left, right, third)
                }
                : defaultCtor(this, left, right, third)
            };
        }
    }
}