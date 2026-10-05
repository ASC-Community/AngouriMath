//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Sets;
using System;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity.Set;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Set
        {
            // TODO:
            partial record FiniteSet
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => Apply(el => el.InnerSimplified(isExact));
            }

            partial record Interval
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => ExpandOnTwoAndTArguments(Left, Right, (l: LeftClosed, r: RightClosed),
                        (a, b, lr) => (a, b, lr) switch
                        {
                            (var left, var right, _) when (isExact ? left.Evaled == right.Evaled : left == right) =>
                                lr.l && lr.r
                                ? new FiniteSet(Simplificator.PickSimplest(left, right))
                                : Empty,
                            _ => null
                        },
                        (@this, a, b, lr) => ((Interval)@this).New(a, lr.l, b, lr.r), isExact); // NOTE: Intervals propagate set unlike other set operations
            }

            partial record ConditionalSet
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                {
                    // Deliberately not ExpandOnTwoAndTArguments: this node binds Var, and that
                    // helper lifts a Providedf out of an argument onto the whole expression.
                    // A predicate's condition is a statement about the bound variable, so
                    // lifting it puts Var outside its own binder -- `{ x : 1/x = 0 }` came back
                    // as `{ } provided not x = 0`, where the x named in the condition is no
                    // longer the x the set ranges over.
                    // https://github.com/asc-community/AngouriMath/issues/878

                    // A calculus operator taken over some other name reads this one as free of
                    // it -- derivative(y, x) is 0 because y is not x. Under a binder over y that
                    // reading is not available: y ranges over values here, and expressions in x
                    // are among them. Simplifying anyway settles a condition that was written to
                    // stay open: `{ y : derivative(y, x) + y - x = 0 }` became `{ y : y - x = 0 }`,
                    // which names x, and putting x back into the condition it came from gives 1.
                    // https://github.com/asc-community/AngouriMath/issues/964
                    if (Var is Variable bound && CalculusOperator.NamesAssumedFreeOf(Predicate, bound).Count > 0)
                        return this;

                    var predicate = Predicate.InnerSimplified(isExact);

                    // `expr provided a provided b` is what the rules build where two operands
                    // each need a condition, so the chain is read to the end rather than one
                    // layer down.
                    var condition = (Entity)Boolean.True;
                    var core = predicate;
                    while (core is Providedf(var inner, var predicated))
                    {
                        condition = condition == Boolean.True ? predicated : condition & predicated;
                        core = inner;
                    }

                    // Membership is the predicate holding, so anything short of True admits
                    // nothing: a predicate that is False where it is defined and undefined
                    // elsewhere -- which is what a condition on False says -- has no members.
                    // Where the predicate is True under a condition, the members are exactly
                    // the points the condition admits, and it becomes the predicate rather than
                    // escaping to the outside of the set.
                    return core.Evaled switch
                    {
                        Boolean(false) => Empty,
                        // A predicate that is not a statement at all -- `x subset 2`, a subset
                        // of a number -- is NaN, and holds nowhere; left in the set it would
                        // make the set NaN, and `Solve` then cast a number to a set and threw.
                        Number { IsNaN: true } => Empty,
                        Boolean(true) when condition != Boolean.True => New(Var, condition),
                        // No set names every value a symbol could take, so a predicate that
                        // holds everywhere is left as written rather than asking for the set of
                        // Domain.Any, which does not exist.
                        Boolean(true) => Codomain is AngouriMath.Core.Domain.Any
                            ? New(Var, Boolean.True)
                            : SpecialSet.Create(Codomain),
                        _ => PreImage(predicate, isExact) ?? Listed(predicate, isExact) ?? New(Var, predicate)
                    };
                }

                /// <summary>
                /// <c>{ x in S : p }</c> for a set of numbers <c>S</c>, listed or written as intervals
                /// where that takes no search: over a listed <c>S</c>, the members at which <c>p</c> is
                /// decided true; over the reals, an interval or a set of whole numbers, <c>S</c> met with
                /// the solutions of <c>p</c>, where <c>p</c> compares rational functions of <c>x</c> of
                /// low degree with numbers for coefficients, or is an equation in moduli of linear
                /// functions, which the solvers answer exactly. A set of whole numbers is met only where
                /// that comes out listed. Sullivan and Mackey's §3.3.3-4, Prop 3.9.6 and Prob 3.11.4:
                /// <c>{ x in ZZ+ : x + 8/x &lt;= 6 }</c> is <c>{ 2, 3, 4 }</c>, and
                /// <c>{ x in RR : x^2 - 2 = 0 }</c> is <c>{ -sqrt(2), sqrt(2) }</c>.
                /// https://github.com/asc-community/AngouriMath/issues/1409
                /// </summary>
                private Entity? Listed(Entity predicate, bool isExact)
                {
                    if (Var is not Variable x || Declared(predicate, x) is not (var declared, var rest) || !rest.ContainsNode(x))
                        return null;
                    if (declared is FiniteSet listed)
                        return Increasing(Filtered(listed, x, rest, isExact));
                    if (!WithinTheReals(declared) || !ComparesRationalFunctions(rest, x))
                        return null;
                    var solved = rest is Equalsf(var left, var right) && (left - right).Nodes.Any(node => node is Absf(var argument) && argument.ContainsNode(x))
                        ? Functions.Algebra.AnalyticalSolving.ModulusSolver.SolveOverTheReals(left - right, x)
                        : Functions.Algebra.AnalyticalSolving.StatementSolver.Solved(rest, x);
                    return solved is null or ConditionalSet ? null : Increasing(Met(declared, solved, isExact));
                }

                /// <summary>
                /// A listed set or a union of numbers as <see cref="SetOperators.CanonicalUnion"/> writes
                /// it, which is how it reads: the listing's pieces met one by one come out in the order
                /// they met.
                /// </summary>
                private static Entity? Increasing(Entity? set)
                    => set is Set numbers && SetOperators.CanonicalUnion(numbers) is { } canonical ? canonical : set;

                /// <summary>The membership a set builder's predicate declares, wherever it sits among the conjuncts, and the rest.</summary>
                private static (Set Declared, Entity Condition)? Declared(Entity predicate, Variable x)
                {
                    var conjuncts = Andf.LinearChildren(predicate).ToList();
                    var at = conjuncts.FindIndex(conjunct => conjunct is Inf(var name, Set) && name == x);
                    if (at < 0)
                        return null;
                    var declared = (Set)((Inf)conjuncts[at]).SupSet;
                    conjuncts.RemoveAt(at);
                    return (declared, conjuncts.Count == 0 ? Boolean.True : conjuncts.Aggregate(static (left, right) => left & right));
                }

                /// <summary>The members of a listed set at which <paramref name="rest"/> is decided true; <see langword="null"/> where one is not decided.</summary>
                private static Entity? Filtered(FiniteSet listed, Variable x, Entity rest, bool isExact)
                {
                    if (listed.Count > LargestFiltered)
                        return null;
                    var kept = new System.Collections.Generic.List<Entity>();
                    foreach (var member in listed)
                        switch (rest.Substitute(x, member).InnerSimplified(isExact).Evaled)
                        {
                            case Boolean(true): kept.Add(member); break;
                            case Boolean(false): break;
                            default: return null;
                        }
                    return new FiniteSet(kept);
                }

                /// <summary>How many members of a listed set are asked about before the set builder is left as written.</summary>
                private const int LargestFiltered = 4096;

                private static bool WithinTheReals(Set set)
                    => set is Interval { IsNumeric: true } || set is SpecialSet special
                        && special.ToDomain() is AngouriMath.Core.Domain.Real or AngouriMath.Core.Domain.Rational or AngouriMath.Core.Domain.Integer
                            or AngouriMath.Core.Domain.NonNegativeInteger or AngouriMath.Core.Domain.PositiveInteger or AngouriMath.Core.Domain.Prime;

                /// <summary>
                /// Whether every comparison in the statement, under <c>and</c>, <c>or</c> and <c>not</c>,
                /// compares rational functions of <paramref name="x"/> of degree at most four together,
                /// with numbers for coefficients -- a modulus of a linear function counting as degree one.
                /// </summary>
                private static bool ComparesRationalFunctions(Entity statement, Variable x)
                    => statement switch
                    {
                        Andf(var left, var right) => ComparesRationalFunctions(left, x) && ComparesRationalFunctions(right, x),
                        Orf(var either, var other) => ComparesRationalFunctions(either, x) && ComparesRationalFunctions(other, x),
                        Notf(var negated) => ComparesRationalFunctions(negated, x),
                        Equalsf or Greaterf or GreaterOrEqualf or Lessf or LessOrEqualf
                            when statement is IBinaryNode { NodeFirstChild: var left, NodeSecondChild: var right }
                            => Degree(left, x) is { } leftDegree && Degree(right, x) is { } rightDegree && leftDegree + rightDegree <= 4,
                        _ => false,
                    };

                /// <summary>
                /// A bound on the degree of <paramref name="expr"/> as a rational function of
                /// <paramref name="x"/> once its denominators are cleared, or <see langword="null"/>
                /// where it is not one: a quotient counts both of its sides.
                /// </summary>
                private static int? Degree(Entity expr, Variable x)
                    => expr switch
                    {
                        _ when !expr.ContainsNode(x) => expr.Vars.Any() ? null : 0,
                        Variable => 1,
                        Sumf(var augend, var addend) => Larger(Degree(augend, x), Degree(addend, x)),
                        Minusf(var minuend, var subtrahend) => Larger(Degree(minuend, x), Degree(subtrahend, x)),
                        Mulf(var multiplier, var multiplicand) => Added(Degree(multiplier, x), Degree(multiplicand, x)),
                        Divf(var dividend, var divisor) => Added(Degree(dividend, x), Degree(divisor, x)),
                        Powf(var @base, Number.Integer { EInteger: var power }) when power.Abs().CompareTo(4) <= 0
                            => Degree(@base, x) is { } d ? d * power.Abs().ToInt32Checked() : null,
                        Absf(var argument) => Degree(argument, x) is 1 ? 1 : null,
                        _ => null,
                    };

                private static int? Larger(int? one, int? other) => one is { } a && other is { } b ? System.Math.Max(a, b) : null;

                private static int? Added(int? one, int? other) => one is { } a && other is { } b ? a + b : null;

                /// <summary>
                /// <paramref name="declared"/> met with the solutions, piece by piece: a listed piece by
                /// membership, an interval as the declared interval or the reals meet it, and a set of
                /// whole numbers only where it comes out listed. <see langword="null"/> where a piece
                /// does not.
                /// </summary>
                private static Entity? Met(Set declared, Set solved, bool isExact)
                {
                    switch (solved)
                    {
                        case FiniteSet roots:
                            return MathS.Intersection(declared, roots).InnerSimplified(isExact) as FiniteSet;
                        case Unionf(Set left, Set right):
                            return Met(declared, left, isExact) is { } metLeft && Met(declared, right, isExact) is { } metRight
                                ? MathS.Union(metLeft, metRight).InnerSimplified(isExact) : null;
                        case Interval interval:
                            return declared switch
                            {
                                SpecialSet.Reals => interval,
                                Interval within => MathS.Intersection(within, interval).InnerSimplified(isExact) is (Interval or FiniteSet) and var met ? met : null,
                                SpecialSet special => SetOperators.IntersectSpecialSetAndInterval(special, interval) as FiniteSet,
                                _ => null,
                            };
                        case SpecialSet.Reals:
                            return declared;
                        default:
                            return null;
                    }
                }

                /// <summary>
                /// <c>{ x in A : f(x) in Y }</c>, a pre-image, is the solutions of the membership
                /// cut by <c>A</c> where the statement solver reads it -- a listed <c>Y</c> or an
                /// interval -- and stays written otherwise. This shape only: a set builder is not
                /// solved on evaluation in general, that being a search on every evaluation.
                /// https://github.com/asc-community/AngouriMath/issues/1409
                /// </summary>
                private Entity? PreImage(Entity predicate, bool isExact)
                {
                    if (Var is not Variable x
                        || predicate is not Andf(Inf(var name, Set declared), Inf(var image, Set) membership)
                        || name != x || !image.ContainsNode(x) || image == x)
                        return null;
                    // An inequality the solver cannot read -- sin(x) > 0 -- leaves the set as
                    // written: the solver declines it, and an evaluation does not throw for a set
                    // it cannot list.
                    if (Functions.Algebra.AnalyticalSolving.StatementSolver.Solve(membership, x) is not { } solved || solved is ConditionalSet)
                        return null;
                    return declared.Intersect(solved).InnerSimplified(isExact);
                }
            }

            partial record SpecialSet
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => this;
            }

            partial record Unionf
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => ExpandOnTwoArguments(Left, Right,
                        (a, b) => (a, b) switch
                        {
                            // A union of numbers is written as its disjoint pieces in increasing order,
                            // a point at an open end closing it: x^2 >= x is solved to
                            // { 0, 1 } \/ (-oo; 0) \/ (1; +oo), which is (-oo; 0] \/ [1; +oo). The pieces
                            // meet pairwise below, and pieces that are not neighbours only here.
                            // https://github.com/asc-community/AngouriMath/issues/1409
                            (Set unionLeft, Set unionRight) when (unionLeft is Unionf || unionRight is Unionf)
                                && SetOperators.CanonicalUnion(new Unionf(unionLeft, unionRight)) is { } canonical => canonical,
                            (FiniteSet setLeft, Set setRight) => SetOperators.UniteFiniteSetAndSet(setLeft, setRight),
                            (Set setLeft, FiniteSet setRight) => SetOperators.UniteFiniteSetAndSet(setRight, setLeft),
                            (Interval intLeft, Interval intRight) => SetOperators.UniteIntervalAndInterval(intLeft, intRight),
                            (ConditionalSet csetLeft, ConditionalSet csetRight) => SetOperators.UniteCSetAndCSet(csetLeft, csetRight),
                            (SpecialSet specialLeft, SpecialSet specialRight) => SetOperators.UniteSpecialSets(specialLeft, specialRight),
                            _ => null
                        },
                        (@this, a, b) => ((Unionf)@this).New(a, b), isExact, propagateSet: false);
            }

            partial record Intersectionf
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => ExpandOnTwoArguments(Left, Right,
                        (a, b) => (a, b) switch
                        {
                            (FiniteSet setLeft, Set setRight) => SetOperators.IntersectFiniteSetAndSet(setLeft, setRight),
                            (Set setLeft, FiniteSet setRight) => SetOperators.IntersectFiniteSetAndSet(setRight, setLeft),
                            (Interval intLeft, Interval intRight) => SetOperators.IntersectIntervalAndInterval(intLeft, intRight),
                            (ConditionalSet csetLeft, ConditionalSet csetRight) => SetOperators.IntersectCSetAndCSet(csetLeft, csetRight),
                            (SpecialSet specialLeft, SpecialSet specialRight) => SetOperators.IntersectSpecialSets(specialLeft, specialRight),
                            (ConditionalSet cset, Interval interval) => SetOperators.IntersectCSetAndInterval(cset, interval),
                            (Interval interval, ConditionalSet cset) => SetOperators.IntersectCSetAndInterval(cset, interval),
                            (SpecialSet special, Interval interval) => SetOperators.IntersectSpecialSetAndInterval(special, interval),
                            (Interval interval, SpecialSet special) => SetOperators.IntersectSpecialSetAndInterval(special, interval),
                            // A /\ [a; +oo) /\ (-oo; b] groups to the left, so the two intervals never
                            // meet each other: regrouped, they do, and A meets one bounded interval.
                            (Intersectionf(var rest, Interval one), Interval another)
                                => MathS.Intersection(rest, SetOperators.IntersectIntervalAndInterval(one, another)).InnerSimplified(isExact),
                            // An interval meets a union of intervals piece by piece, which is what
                            // an inequality's solution set is: (0; +oo) /\ ((-oo; -2) \/ (0; 1/2))
                            // is (0; 1/2), where as written it met nothing it could read. The
                            // union may have more than two pieces: x^2 - 3 x + 2 >= 0 is solved to
                            // { 2 } \/ (-oo; 1] \/ (2; +oo), and that meets (1; 2) in nothing.
                            (Interval interval, Unionf(var left, var right) union) when MadeOfPieces(union)
                                => MathS.Union(MathS.Intersection(interval, left).InnerSimplified(isExact), MathS.Intersection(interval, right).InnerSimplified(isExact)).InnerSimplified(isExact),
                            (Unionf(var left, var right) union, Interval interval) when MadeOfPieces(union)
                                => MathS.Union(MathS.Intersection(left, interval).InnerSimplified(isExact), MathS.Intersection(right, interval).InnerSimplified(isExact)).InnerSimplified(isExact),
                            // Two such unions meet piece by piece as well, each piece of the one
                            // meeting the other by the arms above. A conjunction of two inequalities
                            // is solved to that: the x > 0 where x^2 - 5 x + 1 > 0 and not
                            // x^2 - 3 x + 1 > 0 are ((0; a) \/ (b; +oo)) /\ ({ c, d } \/ (-oo; 0) \/ (c; d)),
                            // which meet in nothing -- Sullivan and Mackey's Prob 4.11.22.
                            // https://github.com/asc-community/AngouriMath/issues/1409
                            (Unionf(var left, var right) union, Unionf other) when MadeOfPieces(union) && MadeOfPieces(other)
                                => MathS.Union(MathS.Intersection(left, other).InnerSimplified(isExact), MathS.Intersection(right, other).InnerSimplified(isExact)).InnerSimplified(isExact),
                            // (A \ B) /\ C is (A /\ C) \ B, where A meets C first: the pre-image
                            // (RR \ { -1 }) /\ (-oo; -1) is (-oo; -1) \ { -1 }, which is the interval.
                            (SetMinusf(var from, var removed), Set other) when other is not SetMinusf
                                => MathS.SetSubtraction(MathS.Intersection(from, other).InnerSimplified(isExact), removed).InnerSimplified(isExact),
                            (Set other, SetMinusf(var from, var removed)) when other is not SetMinusf
                                => MathS.SetSubtraction(MathS.Intersection(other, from).InnerSimplified(isExact), removed).InnerSimplified(isExact),
                            _ => null
                        },
                        (@this, a, b) => ((Intersectionf)@this).New(a, b), isExact, propagateSet: false);

                /// <summary>Whether the set is intervals and listed sets joined by unions, each of which meets an interval in something it can read.</summary>
                private static bool MadeOfPieces(Entity set)
                    => set is Interval or FiniteSet || set is Unionf(var left, var right) && MadeOfPieces(left) && MadeOfPieces(right);
            }

            partial record SetMinusf
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                protected override Entity InnerSimplify(bool isExact)
                    => ExpandOnTwoArguments(Left, Right,
                        (a, b) => (a, b) switch
                        {
                            (Set setLeft, FiniteSet setRight) => SetOperators.SetSubtractSetAndFiniteSet(setLeft, setRight),
                            (Interval intLeft, Interval intRight) => SetOperators.SetSubtractIntervalAndInterval(intLeft, intRight),
                            (ConditionalSet csetLeft, ConditionalSet csetRight) => SetOperators.SetSubtractCSetAndCSet(csetLeft, csetRight),
                            (SpecialSet specialLeft, SpecialSet specialRight) => SetOperators.SetSubtractSpecialSets(specialLeft, specialRight),
                            _ => null
                        },
                        (@this, a, b) => ((SetMinusf)@this).New(a, b), isExact, propagateSet: false);
            }

            partial record IndexedSetOperation
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                // Folded over a listed index set, largest first so that a chain of unions
                // combines as it goes; over anything else the family stays a family.
                protected override Entity InnerSimplify(bool isExact)
                {
                    var over = Over.InnerSimplified(isExact);
                    var body = Body.InnerSimplified(isExact);
                    // The image of an interval under an expression the name occurs in once,
                    // union({f(x)}, x in I), is f(I) by interval arithmetic, which is exact for
                    // one occurrence and the operations it has images for (#1423): the
                    // reference's 9c/5 + 32 on (0, 100) is (32, 212). Where the arithmetic has
                    // no image the substitution leaves an expression, and the family stays.
                    // The reals are the interval (-oo; +oo) for this purpose.
                    var overAsInterval = over is SpecialSet.Reals ? new Interval(Real.NegativeInfinity, false, Real.PositiveInfinity, false) : over;
                    if (this is IndexedUnionf && overAsInterval is Interval && body is FiniteSet { Count: 1 } image
                        && image.First() is var f && f.Nodes.Count(node => node == Var) == 1
                        && f.Substitute(Var, overAsInterval).InnerSimplified(isExact) is Set imaged)
                        return imaged;
                    // A quotient of polynomials the name occurs in more than once, over the
                    // reals, an interval, or intervals with points taken out, by its critical
                    // points and its limits: x/(1 + x) over RR \ {-1} is RR \ {1}.
                    // https://github.com/asc-community/AngouriMath/issues/1409
                    if (this is IndexedUnionf && Var is Variable name && body is FiniteSet { Count: 1 } single
                        && Functions.ImageByCalculus.Of(single.First(), name, over) is { } byCalculus)
                        return byCalculus;
                    // A family of intervals whose ends are monotone in the index, as the interval
                    // between the extremes of its ends: the intersection of (-1/n; 1/n) over ZZ+ is
                    // { 0 }. https://github.com/asc-community/AngouriMath/issues/1409
                    if (Var is Variable familyIndex && body is Interval familyMember
                        && Functions.ImageByCalculus.Family(this is IndexedUnionf, familyMember, familyIndex, over) is { } family)
                        return family;
                    if (over is FiniteSet indices)
                    {
                        if (indices.Count == 0)
                            return this is IndexedUnionf ? Set.Empty : New(Var, over, body);
                        Entity? folded = null;
                        foreach (var index in indices)
                        {
                            var member = body.Substitute(Var, index);
                            folded = folded is null ? member : this is IndexedUnionf ? folded.Unite(member) : folded.Intersect(member);
                        }
                        return folded!.InnerSimplified(isExact);
                    }
                    return New(Var, over, body);
                }
            }

            partial record Powersetf
            {
                private protected override Entity IntrinsicCondition => Boolean.True;
                /// <inheritdoc/>
                // Listed for a finite argument; a power set of anything else is an object with a
                // membership test and no list.
                protected override Entity InnerSimplify(bool isExact)
                    => ExpandOnOneArgument(Argument,
                        a => a switch
                        {
                            FiniteSet finite => finite.GetPowerSet(),
                            Number or Boolean => MathS.NaN,
                            _ => null
                        },
                        (@this, a) => ((Powersetf)@this).New(a), isExact, propagateSet: false);
            }
        }

        partial record Providedf
        {
            private protected override Entity IntrinsicCondition => Predicate;
            private Entity Decide(Entity expr, Entity predicate)
            {
                if (predicate.Evaled == Boolean.True)
                    return expr;
                if (predicate.Evaled == Boolean.False || predicate.Evaled.IsNaN)
                    return MathS.NaN;
                return New(expr, predicate);
            }
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Expression, Predicate,
                    (a, b) => (a, b) switch
                    {
                        (Providedf exprProvided, Providedf predProvided) =>
                            Decide(exprProvided.Expression, exprProvided.Predicate & predProvided.Predicate & predProvided.Expression),
                        (var expr, Providedf predProvided) => Decide(expr, predProvided.Predicate & predProvided.Expression),
                        (Providedf exprProvided, var pred) => Decide(exprProvided.Expression, pred & exprProvided.Predicate),
                        (var expr, var pred) => Decide(expr, pred),
                    },
                    (@this, a, b) => ((Providedf)@this).New(a, b), isExact);
        }

        partial record Piecewise
        {
            private protected override Entity IntrinsicCondition =>
                Cases.Aggregate((Entity?)null, (acc, curr) => acc is { } ? acc | curr.Predicate : curr.Predicate) ?? Boolean.False;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
            {
                // A predicate that evaluates to NaN -- `i < 0`, the complex numbers not being
                // ordered -- holds no more than a false one does: the case is never taken, and the
                // cases after it are read as they would be. Kept, it made the whole piecewise NaN,
                // so that `piecewise(2x provided not x = 0, 3x^2 provided i - 1 < 0)` had no value
                // at any point, and the derivative of an antiderivative written with an arm for
                // each sign of a quantity that is not real came out NaN where it is `2x`.
                foreach (var oneCase in Cases)
                {
                    if (oneCase.Predicate.Evaled.IsNaN) continue;
                    if (oneCase.Predicate.Evaled is not Boolean) goto notYetDecidable;
                    if (oneCase.Predicate.Evaled == Boolean.True) return oneCase.Expression.InnerSimplified(isExact);
                }
                return MathS.NaN;
            notYetDecidable:
                var res = new List<Providedf>();
                foreach (var (@case, srcCase) in (Cases, Cases.Select(c => c.New(c.Expression.InnerSimplified(isExact), c.Predicate.InnerSimplified(isExact)))).Zip()) {
                    if (@case.Predicate.Evaled == Boolean.False || @case.Predicate.Evaled.IsNaN) continue;
                    var toAdd = srcCase.Expression is Providedf(var inner, var pred) ? new Providedf(inner, (srcCase.Predicate & pred).InnerSimplified(isExact)) : srcCase;

                    // A piecewise takes its first matching case, and both rules below follow
                    // from that alone -- neither needs any predicate to be decidable, which
                    // is what makes them apply where the reduction above does not.
                    // https://github.com/asc-community/AngouriMath/issues/327

                    // A predicate that already guards an earlier case can never reach this
                    // one: wherever it holds, the earlier case is taken. Equality is the
                    // special case; what is asked is whether this predicate *entails* an
                    // earlier one, so that `2 < a` is dropped after `1 < a`. Distributing a
                    // binder over a piecewise produces one case per subset of the conditions
                    // and most of them are unreachable exactly this way.
                    // https://github.com/asc-community/AngouriMath/issues/1212
                    if (res.Any(seen => Functions.PredicateEntailment.Entails(toAdd.Predicate, seen.Predicate)))
                    {
                        // Not `continue` -- a decidably true predicate still ends the list,
                        // and skipping that check here would carry unreachable cases past it.
                        if (@case.Predicate.Evaled == Boolean.True) break;
                        continue;
                    }

                    // Two consecutive cases with one expression are one case guarded by
                    // either predicate. Consecutive is the whole condition: a case with a
                    // different expression sitting between them could be taken instead, and
                    // merging across it would change the value where its predicate holds.
                    if (res.Count > 0 && res[res.Count - 1].Expression == toAdd.Expression)
                        res[res.Count - 1] = new Providedf(toAdd.Expression,
                            (res[res.Count - 1].Predicate | toAdd.Predicate).InnerSimplified(isExact));
                    else
                        res.Add(toAdd);

                    if (@case.Predicate.Evaled == Boolean.True) break;
                }
                return New(res);
            }
        }


        partial record Matrix
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => IsScalar ? AsScalar().InnerSimplified(isExact) :
                Elementwise(e => e.InnerSimplified(isExact));
        }

        partial record Application
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            private static Entity ApplyOthersIfNeeded(Entity outer, LList<Entity> arguments)
                => arguments switch
                {
                    LEmpty<Entity> => outer,
                    var nonEmpty => outer.Apply(nonEmpty)
                };

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ((Expression.InnerSimplified(isExact), Arguments.Map(arg => arg.InnerSimplified(isExact))) switch
                {
                    (var identifier, LEmpty<Entity>) => identifier,
                    (Application(var any, var argsInner), var argsOuter) => any.Apply(argsInner.Concat(argsOuter).ToLList()),

                    (Variable("sin"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Sin(), otherArgs),
                    (Variable("cos"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Cos(), otherArgs),
                    (Variable("tan"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Tan(), otherArgs),
                    (Variable("cotan" or "cot"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Cotan(), otherArgs),
                    (Variable("sec"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Sec(), otherArgs),
                    (Variable("cosec" or "csc"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Cosec(), otherArgs),
                    (Variable("arcsin" or "asin"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arcsin(), otherArgs),
                    (Variable("arccos" or "acos"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arccos(), otherArgs),
                    (Variable("arctan" or "atan"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arctan(), otherArgs),
                    (Variable("arccotan" or "acotan" or "acot" or "arccot"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arccotan(), otherArgs),
                    (Variable("arcsec" or "asec"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arcsec(), otherArgs),
                    (Variable("arccosec" or "arccsc" or "acsc" or "acosec"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Arccosec(), otherArgs),

                    (Variable("sinh" or "sh"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Sinh(x), otherArgs),
                    (Variable("cosh" or "ch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Cosh(x), otherArgs),
                    (Variable("tanh" or "th"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Tanh(x), otherArgs),
                    (Variable("cotanh" or "coth" or "cth"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Cotanh(x), otherArgs),
                    (Variable("sech" or "sch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Sech(x), otherArgs),
                    (Variable("cosech" or "csch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Cosech(x), otherArgs),
                    (Variable("asinh" or "arsinh" or "arsh"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Arsinh(x), otherArgs),
                    (Variable("acosh" or "arcosh" or "arch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Arcosh(x), otherArgs),
                    (Variable("atanh" or "artanh" or "arth"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Artanh(x), otherArgs),
                    (Variable("acoth" or "arcoth" or "acotanh" or "arcotanh" or "arcth"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Arcotanh(x), otherArgs),
                    (Variable("asech" or "arsech" or "arsch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Arsech(x), otherArgs),
                    (Variable("acosech" or "arcosech" or "arcsch" or "acsch"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Hyperbolic.Arcosech(x), otherArgs),

                    (Variable("gamma"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Gamma(x), otherArgs),
                    (Variable("phi"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.PhiFunction(), otherArgs),
                    (Variable("abs"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Abs(), otherArgs),
                    (Variable("sqrt"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Sqrt(x), otherArgs),
                    (Variable("cbrt"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Cbrt(x), otherArgs),
                    (Variable("sqr"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Sqr(x), otherArgs),
                    (Variable("signum" or "sign" or "sgn"), (var x, var otherArgs)) => ApplyOthersIfNeeded(x.Signum(), otherArgs),

                    (Variable("ln"), (var x, var otherArgs)) => ApplyOthersIfNeeded(MathS.Ln(x), otherArgs),
                    (Variable("log") v, (var x, LEmpty<Entity>) args) => New(v, args),
                    (Variable("log"), (var p, (var x, var otherArgs))) => ApplyOthersIfNeeded(MathS.Log(p, x), otherArgs),

                    (Variable("derivative") v, (var expr, LEmpty<Entity>) args) => New(v, args),
                    (Variable("derivative"), (var expr, (var x, var otherArgs))) => ApplyOthersIfNeeded(MathS.Derivative(expr, x), otherArgs),

                    (Variable("integral") v, (_, LEmpty<Entity>) args) => New(v, args),
                    (Variable("integral"), (var expr, (var x, var otherArgs))) => ApplyOthersIfNeeded(MathS.Integral(expr, x), otherArgs),

                    (Variable("limit") v,  ((_, LEmpty<Entity>) or (_, (_, LEmpty<Entity>))) and var args) => New(v, args),
                    (Variable("limit") v, (var expr, (var x, (var to, var otherArgs)))) => ApplyOthersIfNeeded(MathS.Limit(expr, x, to), otherArgs),

                    (Variable("limitleft") v, ((_, LEmpty<Entity>) or (_, (_, LEmpty<Entity>))) and var args) => New(v, args),
                    (Variable("limitleft") v, (var expr, (var x, (var to, var otherArgs)))) => ApplyOthersIfNeeded(MathS.Limit(expr, x, to, ApproachFrom.Left), otherArgs),

                    (Variable("limitright") v, ((_, LEmpty<Entity>) or (_, (_, LEmpty<Entity>))) and var args) => New(v, args),
                    (Variable("limitright") v, (var expr, (var x, (var to, var otherArgs)))) => ApplyOthersIfNeeded(MathS.Limit(expr, x, to, ApproachFrom.Right), otherArgs),

                    (Lambda(var x, var body), (var arg, var otherArgs)) => ApplyOthersIfNeeded(body.Substitute(x, arg), otherArgs),

                    (var exprSimplified, var argsSimplified) => New(exprSimplified, argsSimplified),
                }) switch
                {
                    var thisAgain when ReferenceEquals(thisAgain, this) => this,
                    var newOne => newOne.InnerSimplified(isExact)
                };
        }

        partial record Lambda
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            private static LList<Entity>? ReduceArgList(LList<Entity> args, Variable toReduce)
                => args switch
                {
                    LEmpty<Entity> => null,
                    (var curr, LEmpty<Entity>) when curr == toReduce => LList<Entity>.Empty,
                    (_, LEmpty<Entity>) => null,
                    (var curr, var rest) when curr.FreeVariables.Contains(toReduce) => null,
                    (var curr, var rest) =>
                        (ReduceArgList(rest, toReduce) is { } list)
                        ? curr + list
                        : null
                };
            
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => (Parameter, Body.InnerSimplified(isExact)) switch
                {
                    (var x1, Application(var expr, var args))
                        when !expr.FreeVariables.Contains(x1)
                        && ReduceArgList(args, x1) is { } newArgList
                            => newArgList switch
                            {
                                LEmpty<Entity> => expr,
                                var rest => new Application(expr, rest)
                            },
                    (var x, var body) when body != Body => new Lambda(x, body).InnerSimplified(isExact),
                    _ => this
                };
        }
    }
}
