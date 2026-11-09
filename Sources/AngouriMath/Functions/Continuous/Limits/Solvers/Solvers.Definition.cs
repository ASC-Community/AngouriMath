//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath.Functions.Algebra
{
    using AngouriMath.Core.Exceptions;
    using AngouriMath.Core.Multithreading;
    using Core;
    using static Entity;
    using static Entity.Number;
    internal static partial class LimitFunctional
    {
        /// <summary>
        /// Everything below here reads the expression on its way to <c>+oo</c>, whichever
        /// destination was asked for, so that is stated once and held for the whole computation.
        /// </summary>
        /// <remarks>
        /// It has to be the whole computation rather than the simplification below: the
        /// logarithm pairs that have to be gathered are created *inside* the simplifier's own
        /// candidate search and are gathered there, without ever being a subexpression any of
        /// these methods sees. Measured -- with the approach stated around only the first
        /// simplification, the rule was reached 192 times for <c>lim x-&gt;-oo (x-5)^x / x^x</c>
        /// with the operands <c>-x</c> and <c>-(x+5)</c>, and every one of them found no approach
        /// to check against. https://github.com/asc-community/AngouriMath/issues/721
        /// </remarks>
        private static Entity? SimplifyAndComputeLimitToInfinity(Entity expr, Variable x)
        {
            var outerApproach = EnterApproach(x, Real.PositiveInfinity);
            try { return SimplifyAndComputeLimitToInfinityCore(expr, x); }
            finally { LeaveApproach(outerApproach); }
        }

        /// <summary>
        /// A power of the reciprocal of the variable as a power of the variable: (1/x)^p as
        /// x^(-p), and (-1/x)^p as (-1)^p * x^(-p).
        /// </summary>
        /// <remarks>
        /// Exact on the way to +oo, where x is real and positive, and on the principal branch
        /// for any exponent. A one-sided limit at 0 is taken by putting 0 + 1/x or 0 - 1/x for
        /// x, so these are what the root of the variable turns into, and nothing rewrites them:
        /// x^3 * sqrt(1/x) / (x^2 * (1/x)^(-1/2)), which is 1, took 1.6 s, l'Hopital's rule
        /// differentiating it sixteen deep, and lim x->0+ ln(x) / ln(sqrt(x)) asked it eight
        /// times on the way to 2 and took 16 s.
        /// </remarks>
        private static Entity AsAPowerOfTheVariable(Entity node, Variable x)
            => node switch
            {
                Powf(Divf(Integer one, var v), var p) when one == 1 && v == x && !p.ContainsNode(x) => MathS.Pow(x, -p),
                Powf(Divf(Integer minusOne, var v), var p) when minusOne == -1 && v == x && !p.ContainsNode(x)
                    => MathS.Pow(-1, p) * MathS.Pow(x, -p),
                _ => node
            };

        private static Entity? SimplifyAndComputeLimitToInfinityCore(Entity expr, Variable x)
        {
            expr = expr.Replace(node => AsAPowerOfTheVariable(node, x));
            // The value at the destination, read before the simplification below as well as after
            // it: the substitution is what answers a continuous point in any case, under the same
            // guards for a jump, a cut and an indeterminate power, and simplifying first is the
            // expensive half. x / (tanh(1/2) - tan(x)) at 1, moved out to infinity as
            // x = 1 + 1/x, took 10 s to simplify for a value the substitution reads in
            // milliseconds. A real number only: a complex one can be the value on a cut, and the
            // guards for those after this were written against the simplified form.
            if (LimitSolvers.SolveBySubstitution(expr, x) is { } beforeSimplifying
                && beforeSimplifying.Evaled is Real { IsFinite: true })
                return beforeSimplifying.InnerSimplified;
            expr = expr.Simplify();
            if (expr is Providedf(var expression, _)) expr = expression; // limits operate assuming a continuous expression even though some points may be undefined.

            // The second remarkable limit reads a 1^oo, and it ran once, at the top of
            // ComputeLimit, against the expression as it was written. But a form it reads can
            // be *created* by the simplification above: (x - 5)^x / x^x is a quotient when the
            // rule runs and ((x - 5)/x)^x by the time it gets here, and the substitution below
            // then answers 1^(+oo) with 1 where the limit is e^(-5).
            // https://github.com/asc-community/AngouriMath/issues/738
            //
            // Asked again here, which is where the expression is in the form the solvers will
            // read it in and where the destination is +oo whichever destination was asked for.
            // The rule is the same one and its guards are the same, so this adds no reading it
            // did not already have -- only the second chance to apply it. What comes out goes
            // back through ComputeLimit rather than on to the solvers below, because the
            // rewrite leaves e^(g * (f - 1)) unsimplified and it is the simplification of the
            // exponent that turns it into a limit anything can take.
            //
            // Only an answer, never a refusal: where this finds nothing the solvers below are
            // still asked, and they are what answers everything that is not a 1^oo.
            if (MaySecondRemarkableBeReread)
            {
                secondRemarkableRereads++;
                try
                {
                    if (expr.Replace(node => ApplySecondRemarkable(node, x, Real.PositiveInfinity)) is var reread
                        && reread != expr
                        && ComputeLimit(reread, x, Real.PositiveInfinity) is { } byRemarkable
                        && byRemarkable.Evaled != MathS.NaN)
                        return byRemarkable;
                }
                finally { secondRemarkableRereads--; }
            }

            var substitutionResult = LimitSolvers.SolveBySubstitution(expr, x);
            if (substitutionResult is { }) return substitutionResult;

            var logarithmResult = LimitSolvers.SolveAsLogarithm(expr, x);
            if (logarithmResult is { }) return logarithmResult;

            var polynomialResult = LimitSolvers.SolveAsPolynomial(expr, x);
            if (polynomialResult is { }) return polynomialResult;

            var polynomialDivisionResult = LimitSolvers.SolvePolynomialDivision(expr, x);
            if (polynomialDivisionResult is { }) return polynomialDivisionResult;

            var logarithmDivisionResult = LimitSolvers.SolveAsLogarithmDivision(expr, x);
            if (logarithmDivisionResult is { }) return logarithmDivisionResult;

            // Last, because its cost is another walk of the machinery over an expression no
            // smaller than this one -- unlike the rule above, whose sub-limits are subtrees. The
            // readers before both of them settle the expression where it stands.
            var boundedResult = LimitSolvers.SolveAsBoundedTimesVanishing(expr, x);
            if (boundedResult is { }) return boundedResult;

            // After the rule above and not before it. This one answers NaN, which is the claim
            // that there is no limit, and the squeeze theorem is precisely the case where a
            // factor with no limit of its own still leaves the product with one. Asked first it
            // would settle sin(x) / x as (no limit) / (+oo) before the theorem was reached.
            var oscillationResult = LimitSolvers.SolveAsOscillationWithoutLimit(expr, x);
            if (oscillationResult is { }) return oscillationResult;

            return null;
        }

        /// <summary>
        /// <paramref name="expr"/> with each <c>floor</c>, <c>ceil</c>, <c>round</c> and <c>sgn</c> of
        /// <paramref name="x"/> whose limit from <paramref name="side"/> is a whole number put
        /// in as that number. Each takes only whole values, so where it has a limit it takes
        /// that value on the whole of some interval on that side: the substitution is exact
        /// there, and not an approximation. Without it, <c>floor(x) ^ x</c> at 0+ was read as
        /// the form 0^0 and answered 1 through <c>e ^ (x ln(floor(x)))</c>, where on (0, 1) it
        /// is 0 to a positive power, which is 0.
        /// </summary>
        private static Entity WithStepsAtTheirLimits(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (!expr.Nodes.Any(node => node is Floorf or Ceilf or Roundf or Signumf))
                return expr;
            return expr.Replace(node =>
                node is Floorf or Ceilf or Roundf or Signumf
                && node.ContainsNode(x)
                && node.ComputeLimitDivideEtImpera(x, dest, side) is { } limit
                && limit.Evaled is Integer whole
                ? whole
                : node);
        }

        /// <summary>
        /// The limit where the two one-sided limits agree, asked of each side through the
        /// whole of <see cref="ComputeLimit"/>, or <see langword="null"/> where either side
        /// has no answer, the two differ, or the reading is not of real-valued functions.
        /// </summary>
        /// <remarks>
        /// A two-sided limit is the two one-sided ones agreeing, and the branch that compares
        /// them compares two <c>ComputeLimitDivideEtImpera</c> results -- the bare descent --
        /// where a caller who names a side gets that descent *and* everything behind it:
        /// <see cref="SolveAsIndeterminatePower"/>, l'Hopital's rule, and the substitution
        /// that moves a finite destination out to infinity.
        /// <para/>
        /// **Only under a real codomain, and that is the whole of what makes it sound.** The
        /// promotion cannot be made over the complex plane, because there the one-sided limits
        /// do not stay inside the reals: `lim x->0- x^x` answers 1 from the continuation, so
        /// promoting agreement would give `lim x->0 x^x` the value 1 where `x^x` is not real
        /// to the left of 0 at all, and `LimitTest.TestNoLimit` pins it as non-existent.
        /// Under <see cref="AngouriMath.Core.Domain.Real"/> that side has no value to agree
        /// with, so the case this could get wrong is the case that no longer arises.
        /// <para/>
        /// Asked only where the answer would otherwise be NaN, so nothing that already answers
        /// pays for it, and it can only ever turn a refusal into an answer.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/719">#719</a>,
        /// <a href="https://github.com/asc-community/AngouriMath/issues/596">#596</a>
        /// </remarks>
        private static Entity? WhereBothSidesAgree(Entity expr, Variable x, Entity dest)
        {
            if (bothSidesDepth >= MaxBothSidesDepth
                || MathS.Settings.Codomain.Value is not AngouriMath.Core.Domain.Real && !IsRealOnBothSides(expr, x, dest))
                return null;
            bothSidesDepth++;
            try
            {
                if (ComputeLimit(expr, x, dest, ApproachFrom.Left) is not { } fromLeft
                    || fromLeft.Evaled == MathS.NaN)
                    return null;
                if (ComputeLimit(expr, x, dest, ApproachFrom.Right) is not { } fromRight
                    || fromRight.Evaled == MathS.NaN)
                    return null;
                return fromLeft == fromRight || ExpressionNumerical.AreEqual(fromLeft, fromRight)
                    ? fromLeft : null;
            }
            finally { bothSidesDepth--; }
        }

        /// <summary>
        /// Whether <paramref name="expr"/> is real on both sides of a real <paramref name="dest"/>,
        /// close enough to it, but for its constants, decided structurally: every power to an exponent that is not whole,
        /// and every logarithm, has a base and an argument positive on both sides, by
        /// <see cref="SignNear"/>. Where it is, its one-sided limits are limits of values it takes on
        /// either side, and their agreeing is the two-sided limit under any codomain. <c>x^x</c>
        /// is not, its base being negative on the left, and stays without one; <c>(x^2)^x</c> is,
        /// and was NaN at 0 where each side is 1. Anything else unlisted is not real, which costs
        /// coverage and never correctness.
        /// </summary>
        internal static bool IsRealOnBothSides(Entity expr, Variable x, Entity dest)
        {
            if (dest.Evaled is not Real { IsFinite: true })
                return false;
            return RealThere(expr);
            bool RealThere(Entity node)
            {
                // A constant is the same number on both sides, so a complex one does not make
                // them differ: pi^2 / log(x, -2), which is pi^2 ln(x) / ln(-2), is 0 from each
                // side of 1 and was NaN. What a side could differ in is a part that depends on x,
                // and those are read below as before.
                if (!node.ContainsNode(x))
                    return node.Evaled is Complex { IsFinite: true };
                return node switch
                {
                    Variable variable => variable == x,
                    Sumf(var augend, var addend) => RealThere(augend) && RealThere(addend),
                    Minusf(var minuend, var subtrahend) => RealThere(minuend) && RealThere(subtrahend),
                    Mulf(var multiplier, var multiplicand) => RealThere(multiplier) && RealThere(multiplicand),
                    Divf(var dividend, var divisor) => RealThere(dividend) && RealThere(divisor),
                    Powf(var @base, Integer) => RealThere(@base),
                    Powf(var @base, var exponent) => RealThere(@base) && RealThere(exponent)
                                                     && (NotNegativeThere(@base) || PositiveOnBothSides(@base)),
                    Logf(var @base, var antilogarithm) => RealThere(@base) && RealThere(antilogarithm)
                                                          && PositiveOnBothSides(@base) && PositiveOnBothSides(antilogarithm),
                    // An absolute value is real whatever it is of, and a sine, a cosine and an
                    // arctangent of a real argument are real. An arcsine and an arccosine are real
                    // on [-1, 1], so near the point only where their argument is inside it there.
                    // 1/abs(x)^3 at 0 and sin(arcsin(x)) * (c + 2/x) were NaN for want of these,
                    // where each side agrees with the other.
                    Absf => true,
                    Sinf(var argument) => RealThere(argument),
                    Cosf(var argument) => RealThere(argument),
                    Arctanf(var argument) => RealThere(argument),
                    Arcsinf(var argument) => RealThere(argument) && StrictlyInsideTheUnitInterval(argument),
                    Arccosf(var argument) => RealThere(argument) && StrictlyInsideTheUnitInterval(argument),
                    _ => false
                };
            }
            // Not negative on either side, so that a real power of it is real: what IsNeverNegative
            // reads off the form, an even power of something real, and a real power that is itself
            // real -- sqrt(x^2), whose base vanishes at 0 and is not positive there.
            bool NotNegativeThere(Entity node)
                => IsNeverNegative(node, x)
                   || node is Powf(var @base, Integer { EInteger.IsEven: true }) && RealThere(@base)
                   || node is Powf(_, not Integer) && RealThere(node);
            bool StrictlyInsideTheUnitInterval(Entity node)
                => node.Substitute(x, dest).Evaled is Real { IsFinite: true } value
                   && value.EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.One) < 0;
            bool PositiveOnBothSides(Entity node)
                => !node.ContainsNode(x)
                   ? node.Evaled is Real { IsPositive: true }
                   : SignNear(node, x, dest, ApproachFrom.Left) is 1 && SignNear(node, x, dest, ApproachFrom.Right) is 1;
        }

        /// <summary>
        /// How deep <see cref="WhereBothSidesAgree"/> may nest. Each one-sided limit it asks
        /// for may itself reach a two-sided limit of a subexpression, so without a bound the
        /// two questions would branch into four and so on down the tree, for expressions that
        /// have no answer either way and where the whole cost buys nothing.
        /// </summary>
        private const int MaxBothSidesDepth = 2;

        [System.ThreadStatic] private static int bothSidesDepth;

        /// <summary>
        /// Whether the descent under way is one of the two a two-sided limit at a finite point
        /// compares. A power under it reads 0^0 through its exponent only one-sidedly, and two
        /// sides compared that way make (-x)^x at 0 answer 1 where x^x, its mirror, is NaN: the
        /// branch each side of the point takes is not this reading's to settle.
        /// Cleared for each limit asked inside the descent, which is a question of its own.
        /// </summary>
        [System.ThreadStatic] private static bool comparingSides;

        /// <summary>
        /// The expression whose one-sided descent is under way. The one-sided path reads it
        /// through its exponent itself once the descent has nothing to say, so the descent does
        /// not ask the same question of it first.
        /// </summary>
        [System.ThreadStatic] private static Entity? descentRoot;

        /// <summary>
        /// Whether a power met in the descent may read itself through its exponent: neither in
        /// a two-sided comparison nor as the whole of a one-sided question.
        /// </summary>
        internal static bool MayReadAPowerThroughItsExponent(Entity power)
            => !comparingSides && !ReferenceEquals(power, descentRoot);

        /// <summary>
        /// Whether the descent is reading the two sides of a two-sided limit to compare them. A
        /// side read there is not that side's question, and a product regrouped there would decide
        /// a two-sided limit with a complex side that the comparison leaves undecided.
        /// </summary>
        internal static bool IsComparingSides => comparingSides;

        /// <summary>
        /// Whether <paramref name="expr"/> is the whole of the question whose descent is under
        /// way. The path that began the descent asks the rest of the machinery of it once the
        /// descent has nothing to say, so the descent does not ask its whole limit as well.
        /// </summary>
        internal static bool IsTheWholeQuestion(Entity expr) => ReferenceEquals(expr, descentRoot);

        /// <summary>
        /// A logarithm of a power to a constant real exponent, log(f^c), as c log(f), or as
        /// c log(-f) + log((-1)^c) where f is negative near the point.
        /// </summary>
        /// <remarks>
        /// For f positive and c real, f^c is positive and ln(f^c) is c ln(f). For f negative, f^c is
        /// (-f)^c (-1)^c on the principal branch, a positive number times one of modulus 1, so
        /// ln(f^c) is c ln(-f) + ln((-1)^c), which is c ln(f) only for c in (-1, 1]. In that range the
        /// first form holds wherever f is real, whatever its sign, so a two-sided limit whose sides
        /// differ in sign can still be asked through it. Nothing read ln(f^c) through ln(f):
        /// log(x, arcsin(x)^tan(1/2)) at 0, which is tan(1/2) from each side, was left unevaluated
        /// after three seconds from the right and four two-sided, where log(x, arcsin(x)) was 1 at
        /// once. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=65</c>; no issue was filed.
        /// </remarks>
        private static Entity LogarithmOfAPower(Entity node, Variable x, Entity dest, ApproachFrom side)
        {
            if (node is not Logf(var @base, Powf(var power, var exponent))
                || exponent.ContainsNode(x) || exponent.Evaled is not Real { IsFinite: true } c
                || !power.ContainsNode(x))
                return node;
            if (c > -1 && c <= 1 && IsRealOnEachSide(power, x, dest, side))
                return exponent * new Logf(@base, power);
            return SignOnEachSide(power, x, dest, side) switch
            {
                1 => exponent * new Logf(@base, power),
                -1 => exponent * new Logf(@base, -power) + new Logf(@base, MathS.Pow(-1, exponent)),
                _ => node
            };
        }

        /// <summary>
        /// A power of a power to constant real exponents, (g^a)^b, as g^(a b) where g is positive near the point on each
        /// side approached.
        /// </summary>
        /// <remarks>
        /// For g positive and a, b real, g^a is positive and (g^a)^b is g^(a b). Where g is negative it is not:
        /// sqrt(x^3) is i |x|^(3/2) for x &lt; 0, and x^(3/2) is -i |x|^(3/2). Read as written, sqrt(x^3) / x at 0 from the
        /// right was 0 only after four seconds of l'Hopital's rule, each step another root over a power, where x^(3/2) / x
        /// is sqrt(x) at once; floor(sqrt(x^3)) and sgn(sqrt(x^3)) asked it for the sign of sqrt(x^3). An integer outer
        /// exponent is left to the normalisation, which takes (g^a)^n apart whatever the sign of g. Found by limitcheck;
        /// no issue was filed.
        /// </remarks>
        private static Entity PowerOfAPower(Entity node, Variable x, Entity dest, ApproachFrom side)
            => node is Powf(Powf(var @base, var inner), var outer)
                && @base.ContainsNode(x) && !inner.ContainsNode(x) && !outer.ContainsNode(x)
                && inner.Evaled is Real { IsFinite: true } && outer.Evaled is Real { IsFinite: true } and not Integer
                && SignOnEachSide(@base, x, dest, side) is 1
                    ? MathS.Pow(@base, (inner * outer).InnerSimplified)
                    : node;

        /// <summary>
        /// A logarithm to a base other than e as a quotient of natural logarithms.
        /// </summary>
        /// <remarks>
        /// A base that is e evaluated is e. The limit of a limit node's evaluation is taken of its
        /// expression evaluated, in which ln(f) is log(2.718..., f), and as a quotient that is
        /// ln(f) / ln(2.718...), whose first remarkable limit asks the limit of ln(f) through a
        /// limit node of its own -- which is evaluated, and so asks the same question again. Where
        /// the limit of ln(f) is not decided, that did not end, and the stack overflowed.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2273">#2273</a>
        /// </remarks>
        private static Entity ExpandLogarithm(Entity expr)
            => expr switch
            {
                Logf(var @base, var antilog) when @base == MathS.e.Evaled => MathS.Ln(antilog),
                Logf(var @base, var antilog) when @base != MathS.e => MathS.Ln(antilog) / MathS.Ln(@base),
                _ => expr
            };

        /// <summary>
        /// How many limits a budgeted computation may still take, every one it asks along the way counted, or
        /// <see langword="null"/> where there is no budget. Past it each is undecided. Set by <see cref="WithinABudget"/>.
        /// </summary>
        [System.ThreadStatic] private static int? remainingLimits;

        /// <summary>Whether a caller has set a budget of limits, which a limit asked on the side would spend.</summary>
        internal static bool UnderALimitBudget => remainingLimits is not null;

        /// <summary>
        /// <paramref name="compute"/> with at most <paramref name="limits"/> limits taken in all, for a caller that has a
        /// use only for a limit it can have cheaply, and leaves the question open otherwise.
        /// </summary>
        internal static T WithinABudget<T>(int limits, System.Func<T> compute)
        {
            var enclosing = remainingLimits;
            var start = enclosing is { } outer ? System.Math.Min(outer, limits) : limits;
            remainingLimits = start;
            try { return compute(); }
            finally
            {
                // What was spent here is spent from the enclosing budget too.
                var used = start - (remainingLimits ?? 0);
                remainingLimits = enclosing is { } stillOuter ? stillOuter - used : null;
            }
        }

        public static Entity? ComputeLimit(Entity expr, Variable x, Entity dest, ApproachFrom side = ApproachFrom.BothSides, bool acceptNaN = false)
        {
            if (remainingLimits is { } remaining)
            {
                if (remaining <= 0)
                    return null;
                remainingLimits = remaining - 1;
            }
            // An infinity is approached from one side only, so a side asked for there is the
            // limit itself -- and the two-sided path is where the machinery for an infinite
            // destination lives. Taken one-sidedly, -x + ln(2 * pi * x) / 2 at +oo was left
            // unevaluated where the limit is -oo. A complex infinity is not a real one and is
            // turned away below as before.
            if (side is not ApproachFrom.BothSides && dest is Real { IsFinite: false, IsNaN: false })
                side = ApproachFrom.BothSides;
            // The approach is stated for the whole computation, not only where the destination
            // has been normalised to +oo. Everything below simplifies as it goes, and a rule that
            // may only fire while the approach is in scope is otherwise declining through most of
            // the work -- which is not a wrong answer but is a slower one, since what it declines
            // to collapse the search then has to explore.
            if (expr.Nodes.Any(static node => node is Derivativef))
                expr = WithTheDerivativesTaken(expr, x, dest, side);
            var enclosingApproach = EnterApproach(x, dest);
            var (outerComparing, outerRoot) = (comparingSides, descentRoot);
            (comparingSides, descentRoot) = (false, null);
            try { return ComputeLimitWithinApproach(expr, x, dest, side, acceptNaN); }
            finally
            {
                (comparingSides, descentRoot) = (outerComparing, outerRoot);
                LeaveApproach(enclosingApproach);
            }
        }

        /// <summary>
        /// <paramref name="expr"/> with each derivative node that differentiates in closed form replaced by what
        /// it differentiates to.
        /// </summary>
        /// <remarks>
        /// A derivative is a function of the point it is taken at, and its limit is that function's limit, not the
        /// derivative of the limit. Read as a node, the point was put in before differentiating: derivative(ln(x y), y)
        /// at 0 became the derivative of the constant ln(0) and was answered 0, where it is 1 / y; and
        /// derivative(sqrt(x), x) at 0 put 0 in for the variable too, and came back derivative(0, 0). A derivative
        /// that does not close is left as it is.
        /// <para/>
        /// What it differentiates to is inner-simplified. Differentiating states where the derivative exists, as
        /// 1 / x provided not x = 0 for ln(x), and a limit is taken on the way in and not at the point: the condition is
        /// dropped where it holds on each side approached, and kept, which the limit declines, where it does not.
        /// In a method of its own, so that the closure is not allocated for every limit.
        /// </remarks>
        private static Entity WithTheDerivativesTaken(Entity expr, Variable x, Entity dest, ApproachFrom side)
            => expr.Replace(node =>
                node is Derivativef(var expression, Variable variable, var iterations) && iterations > 0
                && expression.Differentiate(variable, iterations).InnerSimplified is var taken
                && !taken.Nodes.Any(static inner => inner is Derivativef)
                    ? taken is Providedf(var value, var condition)
                      && Sides(dest, side).All(oneSide => HoldsNear(condition, x, dest, oneSide) is true)
                        ? value
                        : taken
                    : node);

        private static Entity? ComputeLimitWithinApproach(Entity expr, Variable x, Entity dest, ApproachFrom side, bool acceptNaN)
        {
            // A piecewise is not continuous and is still something a limit can be taken of: it
            // agrees with one of its cases on the whole of the way in, and that case is
            // continuous. Without it here the descent's reading of one was never reached --
            // https://github.com/asc-community/AngouriMath/issues/536. The gate itself stays,
            // since it is what keeps a statement like `a and x` out of machinery that would
            // differentiate it.
            if (expr is not ContinuousNode and not Variable and not Piecewise)
                return null;

            // Under a real codomain a limit reached only through values the function does not
            // take in the reals is not a limit of it.
            //
            // Before the rewrites below and not after them, which is load-bearing rather than
            // tidy: each of those asks for limits of its own, and under this reading those
            // sub-limits come back unevaluated -- whereupon evaluating the unevaluated limit
            // asks for it again, through the same rewrite, without end. Answering here costs
            // nothing and asks nothing.
            // https://github.com/asc-community/AngouriMath/issues/719
            // A two-sided limit is withdrawn if *either* approach leaves the reals, since it
            // is the two one-sided ones agreeing and one of them is not there to agree.
            // Checking only one direction left lim x->0 sqrt(x) answering 0 while its own
            // left-hand limit had been withdrawn, which is the two halves disagreeing about
            // the same reading.
            if (side is ApproachFrom.BothSides
                ? RealCodomainWithdraws(expr, x, dest, ApproachFrom.Left)
                    || RealCodomainWithdraws(expr, x, dest, ApproachFrom.Right)
                : RealCodomainWithdraws(expr, x, dest, side))
                return null;

            // A quotient whose divisor is 0 all the way in has no value there, and has no limit:
            // (abs(x) - x) / (abs(x) - x) at 1 was 1, from l'Hopital's rule, whose premise is a
            // divisor that is not 0 near the point. Two-sided, only where it has no value on
            // either side, since one side alone still approaches the point.
            if (dest.IsFinite && expr is Divf(_, var divisor)
                && divisor.ContainsNode(x) && divisor.Nodes.Any(static node => node is Absf or Signumf)
                && (side is ApproachFrom.BothSides
                    ? VanishesNear(divisor, x, dest, ApproachFrom.Left) && VanishesNear(divisor, x, dest, ApproachFrom.Right)
                    : VanishesNear(divisor, x, dest, side)))
                return MathS.NaN;

            expr = expr.Replace(ExpandLogarithm);
            expr = expr.Replace(node => LogarithmOfAPower(node, x, dest, side));
            expr = expr.Replace(node => PowerOfAPower(node, x, dest, side));

            // In front of both paths, not only the two-sided one. Each of these is guarded on
            // a two-sided limit, and a two-sided limit that exists is also the limit from
            // either side, so none of them says anything one-sided that it did not already say
            // two-sided. Skipping them is what made (1 + x)^(1/x) at 0+ answer 1: without the
            // second remarkable limit the descent reads it as 1^(+oo) and is definite about
            // it, where the same expression approached from both sides gives e.
            expr = expr.Replace(a => TrivialTrigonometricReplacement(a, x));
            expr = ApplyTrivialTransformations(expr, x, dest, side, (_, exprLim) => exprLim);
            expr = ApplyFirstRemarkableOverFactors(expr, x, dest);
            expr = expr.Replace(c => ApplySecondRemarkable(c, x, dest));
            // A tangent is a quotient as much as a cosecant is a reciprocal, and leaving it
            // written as one function makes it opaque to everything that reads quotients --
            // including the split that puts a difference of fractions over one denominator,
            // which takes tan(x) into the numerator whole and so combines 1/x - 1/tan(x) over
            // x * tan(x). That is the right answer in the wrong form: the rules do reach 0
            // through it, but only after rewriting the whole expression dozens of ways, and
            // lim x->0+ (1/sin(x) - 1/tan(x)) took forty seconds against under one for the
            // same limit over sin(x) * sin(x).
            //
            // After the first remarkable limit and not with the trigonometric rewrite in front
            // of it. That one matches a quotient, and rewriting tan(b*x - x) as a quotient of
            // its own turns the quotient it sits under into a product:
            // lim x->0 sin(x - a*x) / tan(b*x - x) stops being read at all.
            expr = expr.Replace(c => AsSineOverCosine(c, x));
            MultithreadingFunctional.ExitIfCancelled();

            if (side is ApproachFrom.Left or ApproachFrom.Right)
            {
                expr = WithStepsAtTheirLimits(expr, x, dest, side);
                descentRoot = expr;
                Entity? oneSided;
                try { oneSided = expr.ComputeLimitDivideEtImpera(x, dest, side); }
                finally { descentRoot = null; }
                if (oneSided is { } found && (acceptNaN || found.Evaled != MathS.NaN))
                    return oneSided;
                // The same rule the two-sided path falls through to, asked with the side it was
                // given. l'Hopital's rule is stated one-sidedly to begin with -- the two-sided
                // case is the two one-sided ones agreeing -- so this is not a weaker reading of
                // it than the one above. Where the descent has nothing to say about a quotient,
                // it is what answers (1 - cos(x)) / x^2 at 0+ with 1/2 rather than NaN, and
                // csc(x) * x with 1: the csc rewrite leaves a product, which the descent does
                // not take apart but which the rule reads back as the quotient x / sin(x).
                if (SolveAsIndeterminatePower(expr, x, dest, side) is { } byExponent)
                    return byExponent;
                if (ApplylHopitalRule(expr, x, dest, side) is { } lhopital && lhopital.Evaled != MathS.NaN)
                    return lhopital;
                // The one-sided path is the only one with nothing behind it, and the descent
                // can make an indeterminate form definite on the way down: for x * ln(x) at 0+
                // it substitutes the first factor's own limit and then asks for 0 * -oo, which
                // is NaN. NaN is the claim that the limit does not exist, and here it is 0.
                //
                // Moving x out to infinity is what ComputeLimitImpl does for a finite
                // destination in any case, so this asks the same question in the place where
                // l'Hopital's rule and the rest of the machinery for infinity live -- none of
                // which this path can otherwise reach. Only where nothing above answered, and
                // whatever was found before is kept if this finds nothing better.
                // Simplified, and not only for tidiness: the substitution leaves the
                // destination behind as a term, and it is exactly that leftover which makes
                // the answer NaN. x * ln(x) at 0+ becomes (0 + 1/x) * ln(0 + 1/x), whose limit
                // at infinity comes back NaN, where the same expression written as
                // (1/x) * ln(1/x) comes back 0.
                if (dest.IsFinite
                    && ComputeLimit(expr.Substitute(x, side is ApproachFrom.Left ? dest - 1 / x : dest + 1 / x)
                                        .InnerSimplified,
                                    x, Real.PositiveInfinity) is { } byInfinity
                    && byInfinity.Evaled != MathS.NaN)
                    return byInfinity;
                return oneSided;
            }
            if (side is ApproachFrom.BothSides)
            {
                if (!dest.IsFinite)
                {
                    // just compute limit with no check for left/right equality
                    // here approach left will be ignored anyways, as dest is infinite number
                    descentRoot = expr;
                    Entity? atInfinity;
                    try { atInfinity = expr.ComputeLimitDivideEtImpera(x, dest, ApproachFrom.Left); }
                    finally { descentRoot = null; }
                    if (atInfinity is { } found && found.Evaled != MathS.NaN)
                        return found;
                    // l'Hopital's rule was only ever reached for a finite destination, so the
                    // textbook oo/oo cases at infinity -- ln(x) / x, x / e^x, ln(x) / sqrt(x) --
                    // were left with nothing to catch them. The rule is only allowed to improve
                    // on what is already there: sqrt(x) / sqrt(x + 1) merely turns into its own
                    // reciprocal, and a NaN from it would claim the limit does not exist.
                    if (SolveAsIndeterminatePower(expr, x, dest, ApproachFrom.Left) is { } byExponent)
                        return byExponent;
                    if (ApplylHopitalRule(expr, x, dest) is { } lhopital && lhopital.Evaled != MathS.NaN)
                        return lhopital;
                    // The rewrites are worth their cost only where there is no answer without
                    // them, and each of them costs an expansion or a simplification of the
                    // whole expression. The descent visits every part of every expression, so
                    // rather than pay for them at each of those parts on every limit ever
                    // taken, the descent is walked a second time with them turned on, and only
                    // for an expression that has just been found to have no answer at all.
                    if (SolveByRewriting(expr, x, dest) is { } rewritten && rewritten.Evaled != MathS.NaN)
                        return rewritten;
                    // Last, because it is the most expensive and everything above is cheaper
                    // for the cases it answers. What it adds are the ones nothing above has
                    // any reading of, including the differences whose terms cancel to every
                    // order: e^(x + e^(-x)) - e^x is 1, and no amount of differentiating both
                    // parts of it arrives there.
                    //
                    // Only where the destination is itself infinite, which is what the
                    // algorithm is stated for. A limit at a finite point reaches infinity too,
                    // by substituting for x, but there it would be replacing answers the rules
                    // above already give rather than adding ones they do not -- a change worth
                    // making on its own evidence and not as a side effect of this.
                    // Gruntz reads everything on its way to +oo -- a negative destination is
                    // normalised by substituting -x, exactly as above -- and it simplifies as it
                    // goes, so the approach is stated for it too. This is the one that carries
                    // lim x->-oo (x-5)^x / x^x: what the substitution leaves is
                    // ln(-x) - ln(-(x+5)), both operands negative, gathered inside the
                    // simplifier and nowhere a rewrite of ours can reach.
                    var gruntzApproach = EnterApproach(x, Real.PositiveInfinity);
                    Entity? byGruntz;
                    try
                    {
                        byGruntz = Gruntz.LimitToPositiveInfinity(
                            dest.Evaled is Real { IsNegative: true } ? expr.Substitute(x, -x) : expr, x);
                    }
                    finally { LeaveApproach(gruntzApproach); }
                    if (byGruntz is { } && byGruntz.Evaled != MathS.NaN)
                        return byGruntz;
                    // What the descent found is kept where nothing above did better, an
                    // indeterminate form included: 0 / abs(0) reads as NaN, and NaN is what
                    // stops every caller from searching further. It is not the answer, and it
                    // is withheld where the answer is made, in Transformation.LimitAt -- not
                    // here, where withholding it let the search above run for minutes on
                    // (sin(x) - x) / x^3 at 0 alone.
                    // https://github.com/asc-community/AngouriMath/issues/1186
                    return atInfinity;
                }
                // Each side with its own steps put in: floor(x) ^ x at 0 is 1 from the left and 0
                // from the right, and read bare both sides were 1.
                else if (DescendComparingSides(ApproachFrom.Left) is { } fromLeft
                  && DescendComparingSides(ApproachFrom.Right) is { } fromRight)
                {
                    if (fromLeft == fromRight && (acceptNaN || fromLeft.Evaled != MathS.NaN))
                        return fromLeft;
                    if (ExpressionNumerical.AreEqual(fromLeft, fromRight) && (acceptNaN || fromLeft.Evaled != MathS.NaN))
                        return fromLeft;
                    if (ApplylHopitalRule(expr, x, dest) is { } lhopital
                        && ComputeLimit(lhopital, x, dest, acceptNaN: true) is { } byRule
                        && (acceptNaN || byRule.Evaled != MathS.NaN))
                        return byRule;
                    // Asking each side properly, which is what the two above were not asked.
                    // https://github.com/asc-community/AngouriMath/issues/719
                    // Only where a side came back NaN. A side the descent answered with a value is
                    // what asking that side outright answers too, since the one-sided path returns
                    // the descent's value before anything else, so two values that differ differ
                    // again: tan(x/2) at pi, +oo and -oo, asked both sides through the whole of the
                    // machinery each time, and a definite integral over [0, pi] spent its budget of
                    // limits on that.
                    if ((fromLeft.Evaled == MathS.NaN || fromRight.Evaled == MathS.NaN)
                        && WhereBothSidesAgree(expr, x, dest) is { } agreed)
                        return agreed;
                    return MathS.NaN; // A two-sided limit cannot exist if the limit from left and right don't match.
                }
                else
                {
                    if (ApplylHopitalRule(expr, x, dest) is { } lhopital
                        && ComputeLimit(lhopital, x, dest) is { } byRule)
                        return byRule;
                    // The same promotion as above. This branch is reached where the descent
                    // has nothing to say about one of the sides at all, rather than saying
                    // something that disagrees -- which is most of what the one-sided
                    // fallbacks are for, so it is where the promotion is worth the most.
                    // https://github.com/asc-community/AngouriMath/issues/719
                    return WhereBothSidesAgree(expr, x, dest);
                }
            }
            throw new AngouriBugException($"Unresolved enum parameter {side}");

            Entity? DescendComparingSides(ApproachFrom oneSide)
            {
                var stepped = WithStepsAtTheirLimits(expr, x, dest, oneSide);
                comparingSides = true;
                try { return stepped.ComputeLimitDivideEtImpera(x, dest, oneSide); }
                finally { comparingSides = false; }
            }
        }

        internal static Entity? ComputeLimitImpl(Entity expr, Variable x, Entity dist, ApproachFrom side) => dist switch
        {
            _ when !expr.ContainsNode(x) => expr,
            // avoid NaN values as non finite numbers
            { IsNaN: true } => MathS.NaN,
            // if x -> -oo just make -x -> +oo
            Real { IsFinite: false, IsNegative: true } => SimplifyAndComputeLimitToInfinity(expr.Substitute(x, -x), x),
            // compute limit for x -> +oo
            Real { IsFinite: false, IsNegative: false } => SimplifyAndComputeLimitToInfinity(expr, x),
            Complex { IsFinite: false } =>
                throw new LimitOperationNotSupportedException($"Complex infinities are not supported in limits: lim({x} -> {dist}) {expr}"),
            _ => SimplifyAndComputeLimitToInfinity(side switch
            {
                // lim(x -> 3-) x <=> lim(x -> 0+) 3 - x <=> lim(x -> +oo) 3 - 1 / x
                ApproachFrom.Left => expr.Substitute(x, dist - 1 / x),
                // lim(x -> 3+) x <=> lim(x -> 0+) 3 + x <=> lim(x -> +oo) 3 + 1 / x
                ApproachFrom.Right => expr.Substitute(x, dist + 1 / x),
                _ => throw new System.ArgumentOutOfRangeException(nameof(side), side,
                    $"Only {ApproachFrom.Left} and {ApproachFrom.Right} are supported.")
            }, x)
        };
    }
}