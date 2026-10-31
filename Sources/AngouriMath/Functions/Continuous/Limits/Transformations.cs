//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Multithreading;
using PeterO.Numbers;
using System;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    partial class LimitFunctional
    {
        /// <summary>
        /// A vanishing function written as the vanishing argument it is equivalent to, where
        /// that argument is what vanishes. <c>sin(u) / u</c>, <c>tan(u) / u</c>,
        /// <c>arcsin(u) / u</c> and <c>arctan(u) / u</c> all tend to 1 as u tends to 0, so one
        /// may be written for the other in a product or a quotient without changing its limit.
        /// </summary>
        /// <remarks>
        /// The argument's own limit is what the equivalence needs, not the function's. sin(x)
        /// vanishes at pi as surely as at 0 and is not equivalent to x there -- it is equivalent
        /// to pi - x -- so rewriting it as x turned <c>lim x-&gt;pi sin(x) / (x - pi)</c>, which
        /// is -1, into pi / 0.
        /// </remarks>
        private static Entity EquivalenceRules(Entity expr, Variable x, Entity dest)
            => expr is (Sinf or Tanf or Arcsinf or Arctanf)
               && EvalAssumingContinuous(expr.DirectChildren[0].Limit(x, dest)) == 0
                ? expr.DirectChildren[0]
                : expr;
        private static Entity EvalAssumingContinuous(Entity expr) =>
            expr.Evaled switch
            {
                Providedf(var inner, _) => inner,
                var x => x
            };
        /// <summary>
        /// How close in the approach is sampled when asking whether a function stays real on
        /// the way to its destination. Powers of ten rather than a fixed step, because what
        /// matters is near and not evenly spaced: <c>sqrt(x - 1)</c> is real just to the left
        /// of 2 and not just to the left of 1, and only the nearer samples tell those apart.
        /// </summary>
        /// <remarks>
        /// It stops at a thousandth, and the reason is cost rather than principle. The sample
        /// point goes into the expression, and an expression may put it in an exponent:
        /// <c>(1 + x)^(1/x)</c> at 1e-9 is <c>1.000000001</c> raised to a billion, evaluated
        /// at a hundred digits, and asking that six times on every limit the machinery takes
        /// turned a 20 ms limit into a timeout.
        /// <para/>
        /// Sampling less far in only ever *misses* a function that leaves the reals nearer the
        /// destination than this reaches, and a miss leaves the limit answered exactly as it
        /// was before. The guard withdraws on evidence and never on the absence of it, so the
        /// cheap end of that trade is the safe one.
        /// </remarks>
        [ConstantField] private static readonly int[] ApproachScales = { 1, 2, 3 };

        /// <summary>
        /// Whether the expression takes values outside the reals on the way in. Under a real
        /// codomain such a limit has no value, rather than the value its complex continuation
        /// approaches.
        /// </summary>
        /// <remarks>
        /// <c>lim x-&gt;0- ln(x)</c> is the plain case: the logarithm of a negative real is
        /// <c>ln|x| + i*pi</c>, and answering <c>-oo</c> reports the magnitude of something
        /// that is not a real number at any point of the approach.
        /// <para/>
        /// Decided by sampling rather than symbolically, and deliberately: what is being asked
        /// is whether the function *takes* non-real values near a point, which no property of
        /// the tree answers. The direction of error is chosen too. A sample that cannot be
        /// evaluated, or that comes back non-finite, is passed over rather than counted, and an
        /// expression carrying a second variable is not judged at all -- so the answer is only
        /// ever "yes, demonstrably" or "not shown", and a limit is withdrawn on evidence.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/719">#719</a>
        /// </remarks>
        private static bool LeavesTheRealsOnTheApproach(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (expr.Vars.Count() != 1)
                return false;
            var towardsNegative = side is ApproachFrom.Left;
            if (!dest.IsFinite)
                towardsNegative = dest.Evaled is Real { IsNegative: true };
            foreach (var scale in ApproachScales)
            {
                var ten = EInteger.FromInt32(10).Pow(scale);
                Entity point;
                if (dest.IsFinite)
                {
                    var step = (Entity)Rational.Create(EInteger.One, ten);
                    point = towardsNegative ? dest - step : dest + step;
                }
                else
                {
                    var far = (Entity)Integer.Create(ten);
                    point = towardsNegative ? -far : far;
                }
                if (expr.Substitute(x, point).Evaled is not Complex value || !value.IsFinite)
                    continue;
                var imaginary = value.ImaginaryPart.EDecimal.Abs();
                var real = value.RealPart.EDecimal.Abs();
                // Relative, since the point may be evaluated at a scale where an absolute
                // threshold means nothing, and a rounding artefact must not withdraw a limit.
                if (imaginary.GreaterThan(EDecimal.Create(1, -6).Multiply(EDecimal.Max(real, EDecimal.One, null))))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Whether the reading in force is of real-valued functions, in which case a limit
        /// reached only through the complex plane is not one.
        /// </summary>
        internal static bool RealCodomainWithdraws(Entity expr, Variable x, Entity dest, ApproachFrom side)
            => MathS.Settings.Codomain.Value is AngouriMath.Core.Domain.Real
               && LeavesTheRealsOnTheApproach(expr, x, dest, side);

        private static Entity ApplyFirstRemarkable(Entity expr, Variable x, Entity dest)
            => expr switch
            {
                Divf(var a, var b) div
                    when EvalAssumingContinuous(a.Limit(x, dest)) == 0 && EvalAssumingContinuous(b.Limit(x, dest)) == 0
                        => div.New(EquivalenceRules(a, x, dest), EquivalenceRules(b, x, dest)),

                // A product takes the substitution as much as a quotient does -- it is the ratio
                // of the two forms tending to 1 that licenses it, and that says nothing about
                // which of them the rest of the expression is written over. Only the quotient
                // had it, so lim x->0+ tan(x) * ln(x) went the long way round through l'Hopital's
                // rule and came back NaN, where the same limit written sin(x) * ln(x) is 0.
                Mulf(var a, var b)
                    when EquivalenceRules(a, x, dest) is var equivalentA
                      && EquivalenceRules(b, x, dest) is var equivalentB
                      && (!ReferenceEquals(equivalentA, a) || !ReferenceEquals(equivalentB, b))
                        => equivalentA * equivalentB,

                _ => expr
            };

        /// <summary>
        /// <see cref="ApplyFirstRemarkable"/> applied down the product-and-quotient spine
        /// rather than at the root alone.
        /// </summary>
        /// <remarks>
        /// The rule matches a product or a quotient whose own child is a vanishing sine, so a
        /// constant factor written to the left pushes that sine one level down and out of
        /// reach: <c>2 * sin(1/x) * x</c> parses as <c>(2 * sin(1/x)) * x</c>, whose children
        /// are a product and a variable. The descent then reads it as <c>0 * (+oo)</c> and is
        /// definite about it, while the same product written <c>sin(1/x) * x * 2</c> answers 2.
        /// Which side a caller writes a constant on is not a mathematical difference, and
        /// simplification produces either.
        /// <para/>
        /// **Not a plain <c>Replace</c>, and that is the whole of what keeps it sound.** The
        /// equivalence holds for a *factor* of the expression as a whole -- if <c>f/g -> 1</c>
        /// then <c>f*h</c> and <c>g*h</c> go to the same place -- and not for a term of a sum,
        /// where the difference between <c>f</c> and <c>g</c> is the entire answer. Rewriting
        /// the sine inside <c>(sin(x)/x - 1)/x^2</c> would answer 0 where the limit is -1/6.
        /// Stopping at anything that is not a product or a quotient keeps every rewrite to a
        /// factor of the whole.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/749">#749</a>
        /// </remarks>
        private static Entity ApplyFirstRemarkableOverFactors(Entity expr, Variable x, Entity dest)
        {
            var applied = ApplyFirstRemarkable(expr, x, dest);
            if (applied is not (Mulf or Divf))
                return applied;
            var left = ApplyFirstRemarkableOverFactors(applied.DirectChildren[0], x, dest);
            var right = ApplyFirstRemarkableOverFactors(applied.DirectChildren[1], x, dest);
            // Rebuilt only where a factor actually changed, so an expression the rule has
            // nothing to say about comes back as the very node it went in as, keeping whatever
            // the rest of the machinery has already cached against it.
            if (ReferenceEquals(left, applied.DirectChildren[0])
                && ReferenceEquals(right, applied.DirectChildren[1]))
                return applied;
            return applied is Mulf ? left * right : left / right;
        }

        private static Entity ApplySecondRemarkable(Entity expr, Variable x, Entity dest)
            => expr switch
            {
                // f(x)^g(x) for f(x) -> 1, g(x) -> +oo
                // => (1 + (f(x) - 1)) ^ g(x) = ((1 - (f(x) - 1)) ^ (1 / (f(x) - 1))) ^ (g(x) (f(x) - 1))
                // e ^ (g(x) * (f(x) - 1))
                Powf(var xPlusOne, var xPower) when
                xPlusOne.ContainsNode(x) && xPower.ContainsNode(x) &&
                EvalAssumingContinuous((xPlusOne - 1).Limit(x, dest)) == 0 && DivergesInMagnitude(xPower, x, dest) =>
                MathS.e.Pow(xPower * (xPlusOne - 1)),

                // a(x)^n / b(x)^n, the same limit written as a quotient. The simplifier used
                // to gather this into (a/b)^n for us, which is how the shape reached the rule
                // above at all -- and that gathering is false across the branch cuts, since
                // sqrt(2)/sqrt(-3) is -0.8165i where (2/-3)^(1/2) is +0.8165i.
                // https://github.com/asc-community/AngouriMath/issues/802
                //
                // Read here instead, it is sound: a limit only needs the identity to hold in a
                // neighbourhood of the destination, and both bases are required to be
                // eventually positive on the way there -- where their arguments are both zero
                // and there is no turn of the argument to lose. That is checkable, which is
                // exactly what it is not in the simplifier, where the expression has no
                // destination to be near.
                Divf(Powf(var numeratorBase, var power), Powf(var denominatorBase, var powerAgain)) when
                power == powerAgain && numeratorBase.ContainsNode(x) && denominatorBase.ContainsNode(x)
                && power.ContainsNode(x)
                && IsEventuallyPositive(numeratorBase, x, dest) && IsEventuallyPositive(denominatorBase, x, dest)
                && EvalAssumingContinuous((numeratorBase / denominatorBase - 1).Limit(x, dest)) == 0
                && DivergesInMagnitude(power, x, dest) =>
                MathS.e.Pow(power * (numeratorBase / denominatorBase - 1)),

                _ => expr
            };

        /// <summary>
        /// Whether a base stays positive on the approach to <paramref name="dest"/>, which is
        /// what makes gathering a quotient of two powers into one power sound *here* when it
        /// is not sound in general: two positive bases have argument zero apiece, so their
        /// quotient's argument cannot leave the principal branch.
        /// </summary>
        private static bool IsEventuallyPositive(Entity expr, Variable x, Entity dest)
        {
            var limit = EvalAssumingContinuous(expr.Limit(x, dest));
            return limit == Real.PositiveInfinity || limit is Real { IsPositive: true };
        }

        /// <summary>
        /// The sign an expression settles on approaching <paramref name="dest"/>: <c>1</c> where
        /// it stays positive, <c>-1</c> where it stays negative, <c>0</c> where neither can be
        /// read -- a limit of zero, a non-real one, or none at all.
        /// </summary>
        private static int SignOnTheApproach(Entity expr, Variable x, Entity dest)
        {
            var limit = EvalAssumingContinuous(expr.Limit(x, dest));
            if (limit == Real.PositiveInfinity) return 1;
            if (limit == Real.NegativeInfinity) return -1;
            return limit switch
            {
                Real { IsPositive: true } => 1,
                Real { IsNegative: true } => -1,
                _ => 0
            };
        }

        /// <summary>
        /// <see cref="SignOnTheApproach"/>, asked once per expression per approach. An
        /// <see cref="Entity"/> hashes structurally, so the same operand written the same way is
        /// the same key.
        /// </summary>
        private static int MemoisedSign(Entity expr, Approach approach)
        {
            var key = (approach.Dest, expr);
            if (signMemo is { } memo && memo.TryGetValue(key, out var known))
                return known;
            var sign = SignOnTheApproach(expr, approach.X, approach.Dest);
            if (signMemo is { } store)
                store[key] = sign;
            return sign;
        }

        /// <summary>
        /// How deep <see cref="MayGatherLogarithmsHere"/> may re-enter itself. It asks for a
        /// limit per operand, and those limits run the machinery the question was asked from.
        /// </summary>
        private const int MaxGatherLogarithmsDepth = 1;

        [System.ThreadStatic] private static int gatherLogarithmsDepth;

        /// <summary>
        /// The approach the limit machinery is currently reading an expression on, or
        /// <see langword="null"/> where an expression is being simplified on its own account.
        /// </summary>
        /// <remarks>
        /// This is the one thing a simplification rule cannot work out for itself and the limit
        /// machinery can: *where the expression is going*. <c>ln(a) + ln(b) = ln(a*b)</c> is
        /// false off the positive reals, so <c>Simplify</c> may not apply it to a symbol -- but
        /// on a stated approach the sign of each operand is decidable, and where the two signs
        /// agree the identity is exact. Without this the rule would have to be either unsound
        /// (as it was) or absent, and absent costs termination rather than coverage: the limit
        /// machinery's own expansion creates logarithm pairs that only this can put back
        /// together. https://github.com/asc-community/AngouriMath/issues/721
        /// <para/>
        /// Thread-static because the limit machinery is synchronous and every other setting here
        /// is; it is swapped rather than set, so a nested reading restores the outer one.
        /// </remarks>
        [System.ThreadStatic] private static Approach? currentApproach;

        /// <summary>
        /// A destination being approached, and the signs already established against it.
        /// </summary>
        /// <remarks>
        /// The memo is not an optimisation to be taken or left. The rule is asked once per match
        /// per candidate the simplifier generates, and it is asked about the *same* operands over
        /// and over: <c>lim x-&gt;-oo (x-5)^x / x^x</c> put the same pair to it 192 times. Each
        /// answer costs two limits, which run this whole machinery, so without the memo the
        /// #596 limit stops finishing inside the minute its own test allows it.
        /// </remarks>
        internal readonly record struct Approach(Variable X, Entity Dest);

        /// <summary>
        /// The signs already established, keyed by the destination they were established against
        /// as well as by the expression, and shared by every approach inside the outermost one.
        /// </summary>
        /// <remarks>
        /// A memo per approach is very nearly no memo at all: the limit machinery re-enters
        /// itself constantly and asks about the same operands each time -- one computation put
        /// the same pair to the rule 192 times, and each answer costs two limits, which run this
        /// whole machinery. It is dropped when the outermost approach is left, so nothing
        /// survives a call.
        /// </remarks>
        [System.ThreadStatic] private static Dictionary<(Entity Dest, Entity Expr), int>? signMemo;

        [System.ThreadStatic] private static int approachDepth;

        /// <summary>
        /// Whether an approach is being read, and so whether a simplification can answer
        /// otherwise than it would on its own account: <see cref="MayGatherLogarithmsHere"/> and
        /// <see cref="MayTakeLogOfPowerHere"/> fire only then.
        /// </summary>
        internal static bool IsReadingAnApproach => currentApproach is not null;

        /// <summary>
        /// States that <paramref name="x"/> is being read on its way to <paramref name="dest"/>,
        /// and returns the previous approach for <see cref="LeaveApproach"/> to put back.
        /// </summary>
        internal static Approach? EnterApproach(Variable x, Entity dest)
        {
            var previous = currentApproach;
            approachDepth++;
            signMemo ??= new();
            currentApproach = new Approach(x, dest);
            return previous;
        }

        /// <summary>
        /// Puts back what <see cref="EnterApproach"/> returned, and drops the memo once the
        /// outermost approach is left.
        /// </summary>
        internal static void LeaveApproach(Approach? previous)
        {
            currentApproach = previous;
            if (--approachDepth == 0)
                signMemo = null;
        }

        /// <summary>
        /// Installs <paramref name="approach"/> as the current one and returns the previous, for
        /// the caller to put back.
        /// </summary>
        internal static Approach? SwapApproach(Approach? approach)
        {
            var previous = currentApproach;
            currentApproach = approach;
            return previous;
        }

        /// <summary>
        /// Whether the simplifier's logarithm gathering may fire here, which it may only while
        /// an approach is being read and only where the operands hold their sign on it.
        /// </summary>
        /// <remarks>
        /// The approach is withdrawn for the duration of the check, so that the limits it asks
        /// for cannot come back through this same door -- conservative, and it terminates.
        /// </remarks>
        internal static bool MayGatherLogarithmsHere(Entity left, Entity right, bool isDifference)
        {
            if (currentApproach is not { } approach || gatherLogarithmsDepth >= MaxGatherLogarithmsDepth)
                return false;
            gatherLogarithmsDepth++;
            var previous = SwapApproach(null);
            try
            {
                var leftSign = MemoisedSign(left, approach);
                if (leftSign == 0)
                    return false;
                var rightSign = MemoisedSign(right, approach);
                return isDifference ? leftSign == rightSign : leftSign == 1 && rightSign == 1;
            }
            finally { SwapApproach(previous); gatherLogarithmsDepth--; }
        }

        /// <summary>
        /// Whether <c>log_b(a^c) = c * log_b(a)</c> may be applied here, which it may only while
        /// an approach is being read, and only where <paramref name="base"/> holds a positive
        /// sign on it and <paramref name="exponent"/> is real along it.
        /// </summary>
        /// <remarks>
        /// The identity needs <c>Im(c * ln a)</c> inside the strip <c>(-pi, pi]</c>. A base that
        /// is positive on the approach makes <c>ln a</c> real, and a real <c>c</c> then leaves the
        /// product real, so there is nothing for the principal branch to discard. Both halves are
        /// answerable here and neither is answerable to a simplifier reading the expression on its
        /// own account, which is why the rule declines there.
        /// <para/>
        /// The approach is withdrawn for the duration of the sign check, as in
        /// <see cref="MayGatherLogarithmsHere"/>, so the limits it asks for cannot come back
        /// through this same door.
        /// https://github.com/asc-community/AngouriMath/issues/902
        /// </remarks>
        internal static bool MayTakeLogOfPowerHere(Entity @base, Entity exponent)
        {
            if (currentApproach is not { } approach || logOfPowerDepth >= MaxLogOfPowerDepth)
                return false;
            // A destination off the real line is not an approach along it, and then nothing below
            // can be said about the variable.
            if (approach.Dest.Evaled is not Real)
                return false;
            // The sign check below reads the base's limit, and a limit being positive does not
            // make the base real on the way to it -- x + i*sin(x) tends to +oo off the real line.
            if (!IsRealAlong(@base, approach.X) || !IsRealAlong(exponent, approach.X))
                return false;
            logOfPowerDepth++;
            var previous = SwapApproach(null);
            try
            {
                return MemoisedSign(@base, approach) == 1;
            }
            finally { SwapApproach(previous); logOfPowerDepth--; }
        }

        /// <summary>
        /// How deep <see cref="MayTakeLogOfPowerHere"/> may re-enter itself, for the reason
        /// <see cref="MaxGatherLogarithmsDepth"/> gives: it asks for a limit, and that limit runs
        /// the machinery the question was asked from.
        /// </summary>
        private const int MaxLogOfPowerDepth = 1;

        [System.ThreadStatic] private static int logOfPowerDepth;

        /// <summary>
        /// Whether <paramref name="expr"/> is real wherever <paramref name="x"/> is, decided
        /// structurally. On an approach to a real destination the variable runs along the real
        /// line, which is the one thing that makes this answerable for a symbol at all.
        /// </summary>
        /// <remarks>
        /// A power is admitted only with a whole exponent or a decidably positive base, because a
        /// real raised to a real leaves the real line as soon as the base goes negative:
        /// <c>(-2)^(1/2)</c> is imaginary. Any other symbol answers <see langword="false"/>, since
        /// a second variable carries no approach and may be complex. Anything unlisted answers
        /// <see langword="false"/> as well, which costs coverage and never correctness.
        /// </remarks>
        private static bool IsRealAlong(Entity expr, Variable x)
        {
            // Anything closed that evaluates to a finite real is one, which is how pi and e get
            // in: both are Variable here, and neither is the approach variable.
            if (expr.Evaled is Real { EDecimal.IsFinite: true })
                return true;
            return expr switch
            {
                Variable v => v == x,
                Sumf(var a, var b) => IsRealAlong(a, x) && IsRealAlong(b, x),
                Minusf(var a, var b) => IsRealAlong(a, x) && IsRealAlong(b, x),
                Mulf(var a, var b) => IsRealAlong(a, x) && IsRealAlong(b, x),
                Divf(var a, var b) => IsRealAlong(a, x) && IsRealAlong(b, x),
                Absf => true,
                Powf(var b, Integer) => IsRealAlong(b, x),
                Powf(var b, var e) => IsRealAlong(e, x) && b.Evaled is Real { IsPositive: true },
                _ => false
            };
        }

        /// <summary>
        /// How many times over <see cref="ApplySecondRemarkable"/> may be re-read into an
        /// expression that <see cref="SimplifyAndComputeLimitToInfinity"/>'s simplification
        /// has just created.
        /// </summary>
        /// <remarks>
        /// The rewrite puts the base under an <c>e</c>, which the rule's own pattern requires
        /// to contain the variable and so does not match, meaning it cannot feed itself
        /// directly. This guards the indirect route -- a simplification that reads
        /// <c>e^(g * (f - 1))</c> back as a power whose exponent moves -- and the cost, since
        /// every level of it asks for limits of its own.
        /// </remarks>
        private const int MaxSecondRemarkableRereads = 3;

        [ThreadStatic] private static int secondRemarkableRereads;

        /// <summary>
        /// Whether a re-reading of the second remarkable limit may still be attempted.
        /// </summary>
        private static bool MaySecondRemarkableBeReread => secondRemarkableRereads < MaxSecondRemarkableRereads;

        /// <summary>
        /// The limit of f(x)^g(x) where the pair is indeterminate and the second remarkable
        /// limit does not already cover it -- that is, 0^0 and oo^0 -- or <see langword="null"/>
        /// where that is not the shape or the exponent settles nothing.
        /// </summary>
        /// <remarks>
        /// The descent substitutes each part's own limit, so both of these arrive as 0^0, which
        /// is NaN. Written over as e^(g * ln f), the same question is the limit of a product of
        /// something vanishing with something diverging, which the rules below can take apart.
        /// <para/>
        /// The exponent is asked as a limit of its own rather than rewritten in place, because a
        /// rewrite would only hand the descent a product it reads no better than the power: the
        /// descent substitutes the parts' limits and does not apply l'Hopital's rule to a part.
        /// Asking outright is what puts the whole machinery behind the exponent.
        /// <para/>
        /// 1^oo is left to <see cref="ApplySecondRemarkable"/>, which answers it more directly,
        /// and 0^oo and oo^oo are not indeterminate at all.
        /// </remarks>
        private static Entity? SolveAsIndeterminatePower(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (expr is not Powf(var @base, var power)
                || !@base.ContainsNode(x) || !power.ContainsNode(x)
                || indeterminatePowerDepth >= MaxIndeterminatePowerDepth)
                return null;
            if (EvalAssumingContinuous(power.Limit(x, dest, side)) != 0)
                return null;

            // f^g is e^(g * ln f), and this rule computes the limit of the exponent. Where the
            // base holds a diverging factorial, that logarithm is what Stirling's expansion is
            // stated for -- so the expansion is applied here, to the exponent, rather than to
            // the base, where it would have to reproduce the factorial itself and its merely
            // *relative* error. https://github.com/asc-community/AngouriMath/issues/754
            var byStirling = StirlingExponent(@base, power, x, dest);

            if (byStirling is null)
            {
                var baseLimit = EvalAssumingContinuous(@base.Limit(x, dest, side));
                if (baseLimit != 0 && !IsInfiniteNode(baseLimit))
                    return null;
                // Every route out of ln(f) runs through differentiating f, so a base this
                // library cannot differentiate is one the rewrite cannot finish on: it would
                // only hand the rules an expression with a hole in it and let them work at it.
                // A factorial is the case that matters -- its derivative wants the digamma
                // function, which is not here, and comes back as NaN. That is why the expansion
                // above is tried first: where it applies there is no factorial left to
                // differentiate, and this guard is asking about an expression the rule is no
                // longer going to use.
                var derivative = @base.Differentiate(x).InnerSimplified;
                if (derivative.Nodes.Any(node => node is Derivativef || node == MathS.NaN))
                    return null;
            }

            indeterminatePowerDepth++;
            try
            {
                var exponentExpr = byStirling ?? (power * MathS.Ln(@base)).InnerSimplified;
                if (ComputeLimit(exponentExpr, x, dest, side) is not { } exponent
                    || exponent.Evaled == MathS.NaN)
                    return null;
                return MathS.e.Pow(exponent).InnerSimplified;
            }
            finally { indeterminatePowerDepth--; }
        }

        /// <summary>
        /// <c>power * ln(base)</c> with the logarithm of every diverging factorial in it
        /// replaced by Stirling's expansion, or <see langword="null"/> where there is no such
        /// factorial or the expansion would not be sound here.
        /// </summary>
        /// <remarks>
        /// <c>ln(f!)</c> is <c>f*ln(f) - f + ln(2*pi*f)/2 + 1/(12f) + O(1/f^3)</c>, and what is
        /// dropped **vanishes** -- where the asymptotic for <c>f!</c> itself has an error that
        /// is merely relative. That is why the expansion is written for the logarithm and
        /// applied to this exponent rather than substituted for the factorial in the base.
        /// <para/>
        /// Vanishing is still not sufficient, because the dropped term is multiplied by the
        /// exponent the rewrite sits under: the answer is <c>e^(power * ln(base))</c>, so an
        /// error of <c>1/(12f)</c> in the logarithm contributes <c>power/(12f)</c> to the
        /// exponent. Requiring <c>power / f -> 0</c> is what makes it disappear. For
        /// <c>((x!) / x^x)^(1/x)</c> that ratio is <c>1/x^2</c>.
        /// <para/>
        /// The logarithm has to be taken apart before the factorial's own is visible:
        /// <c>ln(x!/x^x)</c> is one node, and nothing here simplifies it. Splitting it over
        /// products, quotients and powers assumes the parts are positive on the approach, which
        /// is the same assumption the simplifier's <c>ln(a) + ln(b) = ln(a*b)</c> already
        /// makes; it is confined to logarithms that actually hold a diverging factorial, so it
        /// is reached only by expressions that have no answer at all without it.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/754">#754</a>
        /// </remarks>
        private static Entity? StirlingExponent(Entity @base, Entity power, Variable x, Entity dest)
        {
            var factorials = @base.Nodes.OfType<Factorialf>()
                .Where(f => f.Argument.ContainsNode(x)
                            && EvalAssumingContinuous(f.Argument.Limit(x, dest)) == Real.PositiveInfinity)
                .ToList();
            if (factorials.Count == 0)
                return null;
            // The dropped 1/(12f) is multiplied by the exponent this sits under, so it only
            // disappears where power/f does.
            foreach (var factorial in factorials)
                if (EvalAssumingContinuous((power / factorial.Argument).Limit(x, dest)) != 0)
                    return null;
            return (power * LogarithmExpanded(@base, x, dest)).InnerSimplified;
        }

        /// <summary>
        /// Stirling's expansion of <c>ln(f!)</c>, whose error is the vanishing
        /// <c>1/(12f) + O(1/f^3)</c>.
        /// </summary>
        private static Entity StirlingSeries(Entity argument)
            => argument * MathS.Ln(argument) - argument + MathS.Ln(2 * MathS.pi * argument) / 2;

        /// <summary>
        /// The expression with the logarithm of every diverging factorial in it replaced by
        /// Stirling's expansion, or <see langword="null"/> where there is none or the error the
        /// expansion drops would not vanish out of the answer.
        /// </summary>
        /// <remarks>
        /// <see cref="StirlingExponent"/> can state its guard as <c>power / f -> 0</c> because
        /// it knows the shape it is working in: the answer there is
        /// <c>e^(power * ln(base))</c>, so <c>power</c> *is* the coefficient the dropped
        /// <c>1/(12f)</c> gets multiplied by. Anywhere else that coefficient has to be found
        /// rather than read off, and it is found by putting a variable where the logarithm is
        /// and differentiating with respect to it.
        /// <para/>
        /// This is not a rewrite that may be applied wherever a factorial's logarithm appears.
        /// <c>x * (ln(x!) - (x*ln(x) - x + ln(2*pi*x)/2))</c> is <c>1/12</c> -- an expression
        /// built out of the dropped term itself -- and a rewrite that did not ask would answer
        /// it <c>0</c>. There the coefficient is <c>x</c> and <c>x / x</c> does not vanish, so
        /// it is refused. For <c>ln(x!) / x</c> the coefficient is <c>1/x</c> and the ratio is
        /// <c>1/x^2</c>, so it is allowed.
        /// <para/>
        /// The coefficient is put back in terms of the logarithm before it is judged, since an
        /// expression need not be linear in it: for <c>ln(x!)^2</c> the derivative is
        /// <c>2*ln(x!)</c>, which grows like <c>2*x*ln(x)</c> and is refused -- correctly, as
        /// the difference of the squares is <c>ln(x)/6</c> and diverges.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/765">#765</a>
        /// </remarks>
        private static Entity? StirlingRewritten(Entity expr, Variable x, Entity dest)
        {
            var logarithms = expr.Nodes
                .OfType<Logf>()
                .Where(logarithm => logarithm.Base == MathS.e
                                    && DivergingFactorials(logarithm.Antilogarithm, x, dest).Count > 0)
                .Distinct()
                .ToList();
            if (logarithms.Count == 0)
                return null;
            var rewritten = expr;
            foreach (var logarithm in logarithms)
            {
                var probe = Variable.CreateTemp(expr.Vars);
                var coefficient = expr.Substitute(logarithm, probe)
                    .Differentiate(probe).InnerSimplified.Substitute(probe, logarithm);
                if (coefficient.Nodes.Any(node => node is Derivativef || node == MathS.NaN))
                    return null;
                // The logarithm may hold several factorials, and what the expansion drops from
                // it is the sum of their 1/(12f). Each has to vanish against the coefficient.
                foreach (var factorial in DivergingFactorials(logarithm.Antilogarithm, x, dest))
                    if (EvalAssumingContinuous((coefficient / factorial.Argument).Limit(x, dest)) != 0)
                        return null;
                rewritten = rewritten.Substitute(logarithm,
                    LogarithmExpanded(logarithm.Antilogarithm, x, dest));
            }
            return rewritten;
        }

        /// <summary>
        /// Whether an expression is already being read through its own logarithm further up,
        /// in which case doing it again buys nothing and costs a great deal.
        /// </summary>
        [ThreadStatic] private static bool substitutingFactorial;

        /// <summary>
        /// The fixed power of <paramref name="target"/> that <paramref name="expr"/> depends
        /// on, or <see langword="null"/> where there is no such power.
        /// </summary>
        /// <remarks>
        /// Once the expression is read through its logarithm, <c>ln(expr)</c> holds
        /// <c>ln(target)</c> multiplied by exactly this power -- so it is the coefficient the
        /// dropped <c>1/(12f)</c> arrives with, and the same guard as everywhere else applies
        /// to it. Only multiplication, division and a constant power keep a factor a factor; a
        /// sum does not (<c>x! + x</c> is not a fixed power of the factorial at all), and
        /// neither does an exponent that moves -- for <c>(x!)^x</c> the power is <c>x</c>,
        /// <c>x/(12x)</c> does not vanish, and it is refused.
        /// <para/>
        /// Read off the shape rather than by differentiating and simplifying. That is the same
        /// answer at a fraction of the cost: the symbolic route needs a full
        /// <see cref="Entity.Simplify"/> to cancel the target back out -- <c>InnerSimplified</c>
        /// leaves <c>p * (1/x^x) / (p/x^x)</c> standing -- and it is asked on every expression
        /// holding a factorial.
        /// </remarks>
        private static int? PowerItAppearsTo(Entity expr, Entity target, Variable x)
        {
            if (expr == target)
                return 1;
            if (!expr.ContainsNode(target))
                return 0;
            switch (expr)
            {
                case Mulf(var multiplier, var multiplicand):
                    return PowerItAppearsTo(multiplier, target, x)
                         + PowerItAppearsTo(multiplicand, target, x);
                case Divf(var dividend, var divisor):
                    return PowerItAppearsTo(dividend, target, x)
                         - PowerItAppearsTo(divisor, target, x);
                case Powf(var @base, var exponent)
                    when !exponent.ContainsNode(x) && !exponent.ContainsNode(target)
                         && exponent.Evaled is Integer power:
                    return PowerItAppearsTo(@base, target, x) * (int)power;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The distinct factorials in an expression whose argument runs off to infinity, which
        /// are the ones Stirling's expansion is stated for.
        /// </summary>
        private static List<Factorialf> DivergingFactorials(Entity expr, Variable x, Entity dest)
            => expr.Nodes
                .OfType<Factorialf>()
                .Where(factorial => factorial.Argument.ContainsNode(x)
                                    && EvalAssumingContinuous(factorial.Argument.Limit(x, dest))
                                       == Real.PositiveInfinity)
                .Distinct()
                .ToList();

        /// <summary>
        /// The logarithm of an expression holding a diverging factorial, with Stirling's
        /// expansion written into it -- so that the limit is <c>e</c> to the limit of this --
        /// or <see langword="null"/> where there is no such factorial or the expansion would
        /// not be sound.
        /// </summary>
        /// <remarks>
        /// <see cref="StirlingRewritten"/> reaches a factorial only where a logarithm is
        /// already written down, and <c>x! / x^x</c> has none, so that shape had no limit at
        /// all. It is read here by supplying the logarithm: for a positive expression
        /// <c>lim H</c> is <c>e^(lim ln H)</c>, and <c>ln H</c> is where the expansion applies.
        /// <para/>
        /// **Not by substituting <c>e^(Stirling(f))</c> for the factorial**, which is the
        /// obvious move and was measured to be much worse. It puts an <c>e</c> to a large
        /// exponent into the expression, the machinery evaluates that constant to a
        /// hundred-digit decimal, and everything downstream carries it:
        /// <c>lim x-&gt;+oo (x!/e^x)^(1/x)</c> went from half a second to over a minute, on an
        /// expression <see cref="StirlingExponent"/> already answers. Going through the
        /// logarithm keeps the only <c>e</c> in the final answer.
        /// <para/>
        /// The guard is <see cref="PowerItAppearsTo"/>: it is the power of the factorial the
        /// expression depends on, which is exactly the coefficient <c>ln(f!)</c> carries in
        /// <c>ln H</c>, so the dropped <c>1/(12f)</c> has to vanish against it as everywhere
        /// else. <c>(x!)^x</c> gives <c>x</c>, and <c>x/(12x)</c> does not vanish.
        /// </remarks>
        private static Entity? StirlingByItsOwnLogarithm(Entity expr, Variable x, Entity dest)
        {
            var factorials = DivergingFactorials(expr, x, dest);
            if (factorials.Count == 0)
                return null;
            foreach (var factorial in factorials)
            {
                // The power the expression depends on the factorial through is the coefficient
                // the dropped 1/(12f) arrives with, once the whole thing is read through its
                // logarithm -- so it is the same guard as everywhere else, and it is refused
                // where it does not vanish.
                if (PowerItAppearsTo(expr, factorial, x) is not { } power || power == 0)
                    return null;
                if (EvalAssumingContinuous(((Entity)power / factorial.Argument).Limit(x, dest)) != 0)
                    return null;
            }
            return LogarithmExpanded(expr, x, dest);
        }

        /// <summary>
        /// <c>ln(antilogarithm)</c> taken apart over products, quotients and powers, with
        /// Stirling's expansion written for the logarithm of a diverging factorial.
        /// </summary>
        private static Entity LogarithmExpanded(Entity antilogarithm, Variable x, Entity dest)
            => antilogarithm switch
            {
                Factorialf(var argument)
                    when EvalAssumingContinuous(argument.Limit(x, dest)) == Real.PositiveInfinity
                        => StirlingSeries(argument),
                Mulf(var a, var b) => LogarithmExpanded(a, x, dest) + LogarithmExpanded(b, x, dest),
                Divf(var a, var b) => LogarithmExpanded(a, x, dest) - LogarithmExpanded(b, x, dest),
                Powf(var b, var e) when !e.ContainsNode(x) || b.ContainsNode(x)
                    => e * LogarithmExpanded(b, x, dest),
                _ => MathS.Ln(antilogarithm)
            };

        /// <summary>
        /// How deep the rewriting of one power into another may go. The exponent it asks about
        /// is a limit in its own right and may hold a power of the same shape, so without a
        /// bound the work would multiply.
        /// </summary>
        private const int MaxIndeterminatePowerDepth = 3;

        [ThreadStatic] private static int indeterminatePowerDepth;

        /// <summary>
        /// Whether a limit's value is infinite: a real infinity anywhere inside it, or a complex
        /// number with an infinite part, which is what <c>i * abs(x)</c> tends to and which
        /// used to count as finite for being neither <c>+oo</c> nor <c>-oo</c> -- so
        /// <c>i * abs(x) / x</c> was never read as oo/oo and l'Hopital's rule never saw it.
        /// https://github.com/asc-community/AngouriMath/issues/1186
        /// </summary>
        private static bool IsInfiniteNode(Entity expr)
            => expr.ContainsNode("+oo") || expr.ContainsNode("-oo")
               || expr.Nodes.Any(node => node is Complex { IsFinite: false, IsNaN: false });

        /// <summary>
        /// Whether what the machinery handed back is an indeterminate form rather than a value:
        /// <c>0 / abs(0)</c>, <c>(+oo * 0)^2</c> -- a shape assembled out of the parts' limits
        /// that evaluates to NaN without any part of it being NaN. NaN is the claim that there
        /// is no limit, and a form the descent could not read is not that claim; it is the
        /// descent saying nothing. A NaN that was decided -- <c>sin(x)</c> at <c>+oo</c>, and
        /// <c>2 * sin(x)</c> with it -- carries the NaN node itself and is kept.
        /// </summary>
        /// <remarks>
        /// Asked where the answer is made and nowhere inside: internally the form's NaN is
        /// what stops every caller from searching further, and withholding it there let
        /// <c>(sin(x) - x) / x^3</c> at 0 run for minutes.
        /// https://github.com/asc-community/AngouriMath/issues/1186
        /// </remarks>
        internal static bool IsAnIndeterminateForm(Entity expr)
            => expr.Evaled == MathS.NaN && !expr.Nodes.Any(node => node == MathS.NaN);

        /// <summary>
        /// Whether a limit is being taken on behalf of another: the machinery asks for the
        /// limits of its own parts through the same public entry point a caller uses, and an
        /// answer withheld from those is a stop signal withheld from the search.
        /// </summary>
        internal static bool ReadingAnApproach => approachDepth > 0;

        /// <summary>
        /// Whether a power assembled out of the limits of its base and its exponent is an
        /// indeterminate <em>form</em> that this library's arithmetic nonetheless answers with
        /// a value, so that reading the value off would answer the limit wrongly.
        /// </summary>
        /// <remarks>
        /// Substituting each part's own limit into a node and reading the result off is right
        /// wherever the arithmetic is continuous there, and an indeterminate form is exactly
        /// where it is not. Nearly all of them are already declined without any of this,
        /// because they evaluate to NaN and every caller treats NaN as "no limit":
        /// <c>0 * oo</c>, <c>oo - oo</c>, <c>oo / oo</c> and -- the one that matters here --
        /// <c>0^0</c>. That last is why <c>lim x-&gt;0 x^x</c> is NaN, which this library means
        /// deliberately and pins in <c>LimitTest.TestNoLimit</c>, so <c>0^0</c> is **not**
        /// listed here: declining it would turn a considered "does not exist" into "not
        /// settled".
        /// <para/>
        /// The gap is the two forms whose arithmetic gives 1 rather than NaN. <c>oo^0</c> and
        /// <c>1^oo</c> are each the shape of limits with different values -- <c>(1 + 1/x)^x</c>
        /// is <c>e</c> and <c>1^x</c> is 1, both of them <c>1^oo</c> -- and a limit that
        /// assembled one of them read that 1 off as its answer:
        /// <c>lim x-&gt;+oo (x!)^(1/x)</c> came back 1 where it is +oo.
        /// <para/>
        /// This says nothing about what the expression <c>(+oo)^0</c> evaluates to on its own.
        /// That is a question of convention, on which this library agrees with SymPy and with
        /// IEEE 754's <c>pow</c> in answering 1, and it is left exactly as it was.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/754">#754</a>
        /// </remarks>
        internal static bool IsIndeterminatePowerForm(Entity @base, Entity exponent)
        {
            var baseValue = EvalAssumingContinuous(@base);
            var exponentValue = EvalAssumingContinuous(exponent);
            if (exponentValue == 0)
                return IsInfiniteNode(baseValue);
            return baseValue == 1 && IsInfiniteNode(exponentValue);
        }

        /// <summary>
        /// Whether an expression that a destination has already been substituted into holds an
        /// indeterminate power form anywhere in it, in which case its value is not the limit.
        /// </summary>
        internal static bool HoldsAnIndeterminatePowerForm(Entity expr)
            => expr.Nodes.Any(node => node is Powf(var @base, var exponent)
                                      && IsIndeterminatePowerForm(@base, exponent));

        /// <summary>
        /// Whether the exponent grows without bound, which is all the second remarkable
        /// limit needs.
        /// </summary>
        /// <remarks>
        /// Asking only for the two-sided limit is not enough: at <c>x -> 0</c> the
        /// exponent <c>1/x</c> tends to -oo on the left and +oo on the right, so the
        /// two-sided limit does not exist even though the magnitude diverges. That made
        /// <c>lim x-&gt;0 (1 + x)^(1/x)</c> answer 1 rather than e. Both one-sided
        /// limits diverging is enough, because the rewrite below only uses the product
        /// <c>g(x) * (f(x) - 1)</c>, which is well defined from either side.
        /// </remarks>
        private static bool DivergesInMagnitude(Entity power, Variable x, Entity dest)
        {
            if (IsInfiniteNode(power.Limit(x, dest)))
                return true;
            // Only worth asking one side at a time where there are two sides to ask about.
            // Approaching an infinite destination there is only one, so the two further
            // limits would be wasted work and would not mean anything either.
            if (IsInfiniteNode(dest))
                return false;
            return IsInfiniteNode(power.Limit(x, dest, ApproachFrom.Left))
                && IsInfiniteNode(power.Limit(x, dest, ApproachFrom.Right));
        }

        private static bool IsFiniteNode(Entity expr)
            => !IsInfiniteNode(expr) && expr != MathS.NaN;

        /// <summary>
        /// What an inverse trigonometric function tends to where its argument grows without
        /// bound, or <see langword="null"/> where the argument does not diverge or the function
        /// is not one this has a reading for.
        /// </summary>
        /// <remarks>
        /// Neither arcsine nor arccosine is real past 1, and this library reads both on the side
        /// of the cut below the real axis: arcsin(t) is pi/2 - i*arcosh(t) for real t greater
        /// than 1 and -pi/2 - i*arcosh(t) for t less than -1, with arccos being pi/2 - arcsin
        /// throughout. arcosh grows without bound, so what is left in the limit is that real part
        /// with an infinite imaginary one -- https://github.com/asc-community/AngouriMath/issues/333.
        /// <para/>
        /// C99 and Python take the other side of both cuts and so answer the conjugates.
        /// CompiledArcsinBranchTest pins the side this library takes, and a limit that disagreed
        /// with the function it is a limit of would be worse than either convention.
        /// <para/>
        /// An arcsecant is an arccosine of the reciprocal, so a diverging argument takes it to
        /// arccos(0), which is a right angle and is real. Substituting the infinity does settle
        /// that one, but it settles it as arcsec(+oo) -- the right angle written as a function of
        /// an infinity rather than as the right angle.
        /// </remarks>
        internal static Entity? InverseTrigonometryAtInfinity(Entity function, Entity? argumentLimit)
        {
            if (argumentLimit?.Evaled is not Real { IsFinite: false, IsNaN: false } diverging)
                return null;
            var downwards = MathS.i * Real.PositiveInfinity;
            return function switch
            {
                Arcsinf => (diverging.IsNegative ? -MathS.pi / 2 : MathS.pi / 2) - downwards,
                Arccosf => (diverging.IsNegative ? MathS.pi : 0) + downwards,
                Arcsecantf => MathS.pi / 2,
                // The arctangent tends to a right angle with the argument's sign. The arccotangent
                // here is arctan(1/t), with range (-pi/2, pi/2] -- arccot(1000) = 0.000999...,
                // arccot(-1000) = -0.000999..., arccot(0) = pi/2 -- so it tends to 0 either way, and
                // so does the arccosecant, arcsin(1/t): arccsc(1000) = 0.001000..., arccsc(-1000) =
                // -0.001000.... https://github.com/asc-community/AngouriMath/issues/1508
                Arctanf => diverging.IsNegative ? -MathS.pi / 2 : MathS.pi / 2,
                Arccotanf or Arccosecantf => Integer.Zero,
                _ => null
            };
        }

        /// <summary>
        /// Whether putting the parts' own limits in place of the parts settles the whole -- that
        /// is, whether the combination of the two is a number rather than an indeterminate form.
        /// </summary>
        /// <remarks>
        /// The algebra of limits holds for the infinities as much as for the finite values:
        /// where f tends to A and g to B, f * g tends to A * B whenever A * B means anything, and
        /// 1 * +oo means +oo as surely as 2 * 3 means 6. The descent asked only whether both
        /// limits were finite, so the determinate infinite combinations were left to the solvers,
        /// which substitute the destination and read what comes out --
        /// https://github.com/asc-community/AngouriMath/issues/335. That left
        /// <c>lim x-&gt;0+ cos(x) / sin(x)</c> answered and <c>lim x-&gt;0+ cos(x) * (1 / sin(x))</c>
        /// not, the same limit written two ways.
        /// <para/>
        /// The indeterminate forms are exactly the ones the arithmetic calls NaN: 0 * oo,
        /// oo - oo, oo / oo, 0 / 0 and a non-zero over 0, whose answer depends on which side the
        /// divisor vanishes from. Each of those is left to fall through to the readings that can
        /// take it apart.
        /// <para/>
        /// Powers are not asked this question. The arithmetic answers <c>1 ^ (+oo)</c> with 1 and
        /// <c>(+oo) ^ 0</c> with 1, and as limits both are indeterminate -- <c>(1 + 1/x)^x</c>
        /// tends to e -- so a power settled this way would be settled wrongly.
        /// </remarks>
        internal static bool IsDeterminate(Entity combined, Variable x)
            => !combined.ContainsNode(x) && combined.Evaled is Number { IsNaN: false };

        /// <summary>
        /// How many derivatives of the divisor to take looking for the order at which it
        /// vanishes. A divisor that is still flat after four of them is one whose sign this has
        /// no cheap reading of, and reading it wrongly would answer +oo where the truth is -oo,
        /// which is worse than not answering. The forms this is for vanish at the first order
        /// (sin(x), e^x - 1, x - a) or the second (1 - cos(x), x^2).
        /// </summary>
        private const int MaxVanishingOrder = 4;

        /// <summary>
        /// The infinity a quotient tends to when its divisor vanishes and its dividend does not,
        /// or <see langword="null"/> where that is not the shape or the side the divisor
        /// vanishes from cannot be read off.
        /// </summary>
        /// <remarks>
        /// The descent puts each part's own limit in place of the part, and for this shape that
        /// throws away the only thing that decides the answer. <c>cos(x) / sin(x)</c> at 0
        /// becomes <c>1 / 0</c>, which is NaN -- the claim that the limit does not exist -- where
        /// on the right it is +oo and on the left -oo.
        /// <para/>
        /// The side the divisor vanishes from is read off its first derivative that does not
        /// vanish with it. Where <c>g(a) = 0</c> and the first non-vanishing derivative there is
        /// the k-th, <c>g(x)</c> has the sign of <c>g_k(a) * (x - a)^k</c> near a, which is the
        /// sign of <c>g_k(a)</c> on the right and that times <c>(-1)^k</c> on the left. Nothing
        /// is claimed unless a derivative comes out finite and non-zero at the point: an
        /// expression that is not differentiable there, or whose derivative diverges as
        /// <c>sqrt(x)</c>'s does, is left alone.
        /// </remarks>
        internal static Entity? DivergesAtAVanishingDivisor(Entity dividend, Entity divisor, Variable x, Entity dest, ApproachFrom side)
        {
            if (side is not (ApproachFrom.Left or ApproachFrom.Right) || !dest.IsFinite || !divisor.ContainsNode(x))
                return null;
            if (EvalAssumingContinuous(divisor.Limit(x, dest, side)) is not Real { IsZero: true })
                return null;
            // A dividend that vanishes too makes the quotient indeterminate rather than
            // divergent, and one that diverges is a different question again. Only a dividend
            // with a definite non-zero size leaves the divisor to decide the answer.
            if (EvalAssumingContinuous(dividend.Limit(x, dest, side)) is not Real { IsFinite: true, IsZero: false } dividendLimit)
                return null;
            if (SignFromVanishingOrder(divisor, x, dest, side) is not { } divisorSign)
                return null;
            return dividendLimit.IsNegative == divisorSign > 0
                ? Real.NegativeInfinity
                : Real.PositiveInfinity;
        }

        /// <summary>
        /// The sign an expression that vanishes at the destination keeps on one side of it,
        /// or <see langword="null"/> where it cannot be read off.
        /// </summary>
        /// <remarks>
        /// The sign is read off the first derivative that does not vanish with the expression.
        /// Where <c>g(a) = 0</c> and the first non-vanishing derivative there is the k-th,
        /// <c>g(x)</c> has the sign of <c>g_k(a) * (x - a)^k</c> near a, which is the sign of
        /// <c>g_k(a)</c> on the right and that times <c>(-1)^k</c> on the left. Nothing is
        /// claimed unless a derivative comes out finite and non-zero at the point: an expression
        /// that is not differentiable there, or whose derivative diverges as <c>sqrt(x)</c>'s
        /// does, is left alone.
        /// </remarks>
        private static int? SignFromVanishingOrder(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (side is not (ApproachFrom.Left or ApproachFrom.Right) || !dest.IsFinite)
                return null;
            var derivative = expr;
            for (var order = 1; order <= MaxVanishingOrder; order++)
            {
                MultithreadingFunctional.ExitIfCancelled();
                derivative = derivative.Differentiate(x).Simplify();
                var atThePoint = EvalAssumingContinuous(derivative.Substitute(x, dest).InnerSimplified);
                if (atThePoint is not Real { IsFinite: true } value)
                    return null;
                if (value.IsZero)
                    continue;
                // (x - a)^k is positive on the right whatever k is, and on the left it takes the
                // sign of (-1)^k, so approaching from the left at an odd order turns the sign of
                // the derivative around and nothing else does.
                var turnsAround = side is ApproachFrom.Left && order % 2 != 0;
                return value.IsNegative == turnsAround ? 1 : -1;
            }
            return null;
        }

        /// <summary>
        /// The sign an expression keeps throughout a punctured one-sided neighbourhood of the
        /// destination, or <see langword="null"/> where it cannot be read off.
        /// </summary>
        internal static int? SignNear(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (EvalAssumingContinuous(expr.Limit(x, dest, side)) is not Real { IsNaN: false } limit)
                return null;
            // A non-zero limit, infinities included, fixes the sign on its own: the expression
            // is within any margin of that limit close enough to the destination, and one of
            // those margins keeps it on the same side of zero.
            if (!limit.IsZero)
                return limit.IsNegative ? -1 : 1;
            return SignFromVanishingOrder(expr, x, dest, side);
        }

        /// <summary>
        /// Whether a predicate holds throughout a punctured one-sided neighbourhood of the
        /// destination, fails throughout one, or cannot be settled either way
        /// (<see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// This is what a limit of a piecewise expression needs and all it needs: near enough to
        /// the destination one case is the whole of the expression, so the limit is that case's
        /// limit. What decides a comparison is the sign the difference of its two sides keeps
        /// near the destination, which is one question per comparison and not a solution set.
        /// <para/>
        /// A comparison whose difference vanishes identically -- <c>x &lt; x</c>, or any
        /// predicate whose truth changes infinitely often on the way in -- settles nothing here
        /// and comes back null, since neither answer would hold throughout a neighbourhood.
        /// </remarks>
        internal static bool? HoldsNear(Entity predicate, Variable x, Entity dest, ApproachFrom side)
        {
            // A predicate the variable does not occur in is the same statement everywhere, so
            // there is nothing to approach: it either evaluates or it says nothing.
            if (!predicate.ContainsNode(x))
                return predicate.Evaled is Entity.Boolean constant ? constant.Value : null;
            switch (predicate)
            {
                case Entity.Boolean(var value):
                    return value;
                case Notf(var argument):
                    return HoldsNear(argument, x, dest, side) is { } inner ? !inner : null;
                case Andf(var left, var right):
                    {
                        var (l, r) = (HoldsNear(left, x, dest, side), HoldsNear(right, x, dest, side));
                        if (l is false || r is false) return false;
                        return l is true && r is true ? true : null;
                    }
                case Orf(var left, var right):
                    {
                        var (l, r) = (HoldsNear(left, x, dest, side), HoldsNear(right, x, dest, side));
                        if (l is true || r is true) return true;
                        return l is false && r is false ? false : null;
                    }
                // Strict and non-strict read the same here. They differ only where the difference
                // is exactly zero, and a difference that is zero anywhere on the way in is one
                // this has no sign for either way.
                case Lessf(var left, var right):
                    return SignNear(left - right, x, dest, side) is { } lessSign ? lessSign < 0 : null;
                case LessOrEqualf(var left, var right):
                    return SignNear(left - right, x, dest, side) is { } lessOrEqualSign ? lessOrEqualSign < 0 : null;
                case Greaterf(var left, var right):
                    return SignNear(left - right, x, dest, side) is { } greaterSign ? greaterSign > 0 : null;
                case GreaterOrEqualf(var left, var right):
                    return SignNear(left - right, x, dest, side) is { } greaterOrEqualSign ? greaterOrEqualSign > 0 : null;
                // A difference that keeps a sign is never zero, so the two sides are unequal
                // throughout the neighbourhood. The other way round is not readable here: a
                // difference this cannot sign is not thereby zero.
                case Equalsf(var left, var right):
                    return SignNear(left - right, x, dest, side) is { } ? false : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// How deep the rule may be applied to one limit. Differentiating both parts does not
        /// always make the quotient simpler -- x / sqrt(x^2 + 1) turns into sqrt(x^2 + 1) / x,
        /// which turns back, so the two stay indeterminate forever -- and without a bound the
        /// recursion below would not end. The bound has to leave room for the degree of a
        /// polynomial: x^10 / e^x takes ten steps.
        /// </summary>
        private const int MaxlHopitalDepth = 16;

        [ThreadStatic] private static int lHopitalDepth;

        /// <summary>
        /// How many times in all the rule may differentiate a quotient while one limit is being
        /// computed. Bounding the depth alone does not bound the work: each step asks what the
        /// two parts of its quotient tend to, and those are limits the rule may be applied to
        /// in turn, so the steps fan out rather than follow one another, and sixteen deep is
        /// far more than sixteen of them. This is a backstop rather than the answer to that --
        /// the two conditions below are what keep the fan-out from starting.
        /// </summary>
        private const int MaxlHopitalApplications = 64;

        [ThreadStatic] private static int lHopitalApplications;

        [ThreadStatic] private static List<Entity>? lHopitalChain;

        /// <summary>
        /// Whether the rule is already partway through this very quotient. Differentiating both
        /// parts of sqrt(x^2 - x) / x gives its reciprocal, and differentiating that gives the
        /// first back, so the rule can go round for as long as the bound on its depth allows.
        /// Each step of the way costs a simplification and two limits of its own, which is why
        /// bounding the depth is not by itself enough to stop that limit running for a minute.
        /// </summary>
        private static bool AlreadyBeingDifferentiated(Entity quotient)
            => (lHopitalChain ??= new()).Contains(quotient);

        /// <summary>
        /// Whether differentiating both parts has left a quotient bigger than the one it came
        /// from by more than a step on the way to an answer accounts for. Each step asks what
        /// the two parts of its quotient tend to, and a bigger quotient makes those two
        /// questions harder than the one being answered, so a step that grows without bound is
        /// a step away from an answer rather than towards one. One step of
        /// x^(3/2) * sqrt(1 + 1/x^2) / x^2 adds twelve nodes and the next adds twenty-six,
        /// which is that shape.
        /// </summary>
        /// <remarks>
        /// The room was a flat eight nodes, which is what growth looks like when the divisor is
        /// a power of the variable: differentiating that shrinks it, so the only thing that
        /// grows is the dividend and it grows by addition. A divisor that is a *product* of
        /// vanishing factors grows by the product rule instead, which is multiplication --
        /// x^2 * sin(x)^2 differentiates into a sum of two products, each about the size of the
        /// whole. Such a chain still terminates, since the order at which the divisor vanishes
        /// falls by one at every step, but it grows for as long as it takes to get there and
        /// only then collapses: 1/x^2 - 1/sin(x)^2 at 0 goes 17 -> 26 -> 33 -> 46 nodes before
        /// four steps settle it at -1/3. A flat budget cannot say that, and turned the first of
        /// those steps away, leaving the descent to answer +oo - +oo -- NaN, the claim that the
        /// limit does not exist -- for a limit that exists.
        /// <para/>
        /// So the room is the larger of the eight nodes and three fifths again, which is
        /// proportional where the growth is proportional and unchanged for the small quotients
        /// the flat budget was measured on. It is wider than the old rule everywhere, so no
        /// chain that reached an answer before is turned away now. It costs nothing on the
        /// shape that motivated the flat budget either: nineteen nodes to thirty-one is above
        /// three fifths again as surely as it is above eight more, so that chain still stops at
        /// the same step, and the limit still answers 0 in the same 240 ms. The corpus is
        /// unchanged at 112/117 and, in total time, within noise of where it was.
        /// </remarks>
        private static bool GrewTooMuch(Entity quotient, Entity applied)
            => applied.Nodes.Count() > Math.Max(quotient.Nodes.Count() + 8, quotient.Nodes.Count() * 8 / 5);

        [ThreadStatic] private static bool suppresslHopital;

        /// <summary>
        /// One product broken into a numerator and a denominator, with every reciprocal factor
        /// taken into the latter. A product with no reciprocal factor in it comes back with a
        /// denominator of 1, which is what makes this usable term by term below.
        /// </summary>
        /// <remarks>
        /// A power of a quotient is one of these too, and is how a squared cosecant arrives:
        /// csc(x) is written 1/sin(x) in front of the descent, so csc(x)^2 reaches here as
        /// (1/sin(x))^2 with its denominator inside the power rather than at the top. Read as
        /// a factor with no denominator, csc(x)^2 - 1/x^2 at 0 was never put over a common
        /// denominator and came back unevaluated where it is 1/3.
        /// <para/>
        /// Only for a whole power, where (a/b)^n is a^n/b^n exactly. At a half it is not: the
        /// square root of 1/(-1) is i and the quotient of the two square roots is -i, and a
        /// limit taken of the second is a limit of a different function.
        /// </remarks>
        private static (Entity Numerator, Entity Denominator) SplitProduct(Entity expr)
        {
            Entity numerator = 1, denominator = 1;
            foreach (var factor in Mulf.LinearChildren(expr))
                switch (factor)
                {
                    case Powf(Divf(var dividend, var divisor), Integer { IsPositive: true } power):
                        numerator *= dividend.Pow(power);
                        denominator *= divisor.Pow(power);
                        break;
                    case Powf(var @base, Real { IsNegative: true } power):
                        denominator *= @base.Pow(-power);
                        break;
                    case Powf(var @base, Mulf(Real { IsNegative: true } coefficient, var rest)):
                        denominator *= @base.Pow(-coefficient * rest);
                        break;
                    case Divf(var dividend, var divisor):
                        numerator *= dividend;
                        denominator *= divisor;
                        break;
                    default:
                        numerator *= factor;
                        break;
                }
            return (numerator, denominator);
        }

        /// <summary>
        /// How many terms of a sum are worth putting over a common denominator. Each one
        /// multiplies the numerator by every other term's denominator, so the expression grows
        /// with the square of the count, and the rule below has to differentiate whatever comes
        /// out of it. Two and three terms is what the forms that need this are written with.
        /// </summary>
        private const int MaxTermsOverACommonDenominator = 3;

        /// <summary>
        /// The expression rewritten as a single quotient, or <see langword="null"/> where it is
        /// not one to begin with and nothing is gained by writing it as one. The rule below only
        /// reads quotients, and two kinds of expression are quotients without being written as
        /// one.
        /// <para/>
        /// A product with a reciprocal factor: <c>lim x -&gt; +oo x^4 * e^(-x)</c> came out as
        /// NaN while the same limit written <c>x^4 / e^x</c> gave 0 --
        /// https://github.com/asc-community/AngouriMath/issues/596.
        /// <para/>
        /// And a sum of them, which is where the differences of two divergent parts at a finite
        /// point live: <c>1/x - 1/sin(x)</c> at 0 is +oo - +oo on the right and -oo - -oo on the
        /// left, and nothing in the descent takes a difference apart any further than asking
        /// what each half tends to. Over the common denominator it is
        /// <c>(sin(x) - x) / (x * sin(x))</c>, which is 0/0 and which the rule settles at 0 in
        /// three steps.
        /// </summary>
        private static Entity? AsQuotient(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (expr is Mulf)
            {
                // Taking the reciprocal factors out is the better reading where it gives the
                // rule something it can use -- x * e^(-x) comes out as the clean x / e^x -- so
                // it is tried first. But it can also hide the indeterminacy rather than expose
                // it: tan(x) * ln(x) has been written sin(x) / cos(x) * ln(x) by the time it
                // arrives, and splitting on the reciprocal gives sin(x) * ln(x) / cos(x), whose
                // divisor tends to 1. That is no longer a quotient the rule reads, so the other
                // arrangement is tried in its place rather than after it.
                var (numerator, denominator) = SplitProduct(expr);
                if (denominator != 1 && IsIndeterminateQuotient(numerator, denominator, x, dest, side))
                    return numerator / denominator;
                return AsQuotientOfVanishingAndDiverging(expr, x, dest, side)
                    ?? (denominator == 1 ? null : numerator / denominator);
            }
            if (expr is not (Sumf or Minusf))
                return null;
            var terms = Sumf.LinearChildren(expr).ToList();
            if (terms.Count > MaxTermsOverACommonDenominator)
                return null;
            Entity? combined = null, common = null;
            var worthIt = false;
            foreach (var term in terms)
            {
                var (numerator, denominator) = SplitProduct(term);
                // A denominator that does not contain x is a constant factor and putting the sum
                // over it gains nothing, while still costing the rule an expression to
                // differentiate. It is the denominators that vanish or diverge that make the
                // difference indeterminate, and those are the ones that contain x.
                worthIt |= denominator != 1 && denominator.ContainsNode(x);
                (combined, common) = combined is null || common is null
                    ? (numerator, denominator)
                    : (combined * denominator + numerator * common, common * denominator);
            }
            return worthIt && combined is { } && common is { } ? (combined / common).InnerSimplified : null;
        }

        /// <summary>
        /// Whether a quotient is one of the two forms l'Hopital's rule reads: 0/0 or oo/oo.
        /// </summary>
        private static bool IsIndeterminateQuotient(Entity numerator, Entity denominator, Variable x, Entity dest, ApproachFrom side)
        {
            var above = EvalAssumingContinuous(numerator.Limit(x, dest, side));
            var below = EvalAssumingContinuous(denominator.Limit(x, dest, side));
            return above == 0 && below == 0 || IsInfiniteNode(above) && IsInfiniteNode(below);
        }

        /// <summary>
        /// A product of something vanishing with something diverging, written as the quotient
        /// of the diverging factor by the reciprocal of the vanishing one, or
        /// <see langword="null"/> where it is not that shape.
        /// </summary>
        /// <remarks>
        /// This is the other way a product can be indeterminate without being written as a
        /// quotient, and the split above cannot see it: <c>sin(x) * ln(x)</c> has no reciprocal
        /// factor at all, so both halves go into the numerator and it comes back unchanged.
        /// <para/>
        /// The diverging factor goes on top and the vanishing one is inverted underneath, not
        /// the other way round, though both give an indeterminate quotient. Differentiating
        /// <c>ln(x) / csc(x)</c> gets rid of the logarithm and arrives at an answer; the other
        /// arrangement, <c>sin(x) / (1 / ln(x))</c>, differentiates into a product of the same
        /// shape as the one it started from and goes round.
        /// </remarks>
        private static Entity? AsQuotientOfVanishingAndDiverging(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            Entity? vanishing = null, diverging = null;
            Entity rest = 1;
            foreach (var factor in Mulf.LinearChildren(expr))
            {
                if (!factor.ContainsNode(x))
                {
                    rest *= factor;
                    continue;
                }
                var limit = EvalAssumingContinuous(factor.Limit(x, dest, side));
                if (vanishing is null && limit == 0)
                    vanishing = factor;
                else if (diverging is null && IsInfiniteNode(limit))
                    diverging = factor;
                else
                    rest *= factor;
            }
            if (vanishing is not { } zero || diverging is not { } infinity)
                return null;
            // The leftover factors are split rather than multiplied on top, because the children
            // of a product arrive flattened through any division in it: tan(x) * ln(x) comes
            // apart into sin(x), cos(x)^(-1) and ln(x), and putting that cos(x)^(-1) into the
            // numerator would rebuild the very product this is meant to take apart -- the
            // quotient would simplify straight back to it and the rule would be handed its own
            // input. Into the denominator it gives ln(x) / (cos(x) / sin(x)), which is oo/oo
            // and which the rule settles at 0.
            var (restNumerator, restDenominator) = SplitProduct(rest);
            return restNumerator * infinity / (restDenominator * (1 / zero)).InnerSimplified;
        }

        /// <remarks>
        /// The side is carried through because the rule is stated one-sidedly to begin with --
        /// the two-sided case is the two one-sided ones agreeing -- so it is the same rule
        /// either way, asked of the same quotient under the same premises. Carrying it is what
        /// lets the rule answer where the two-sided reading has nothing to say, and the two
        /// questions the rule asks along the way are both of that kind: what the parts tend to,
        /// and what the differentiated quotient tends to. For <c>(1 - cos(x)) / x^3</c> at 0
        /// the second of those reaches <c>sin(x) / (3x^2)</c>, which is +oo on the right and
        /// -oo on the left, so asking about both sides at once gives NaN and the step is
        /// wasted. Asked one side at a time it answers.
        /// </remarks>
        private static Entity? ApplylHopitalRule(Entity expr, Variable x, Entity dest, ApproachFrom side = ApproachFrom.BothSides)
        {
            if (lHopitalDepth == 0)
                lHopitalApplications = 0;
            if (lHopitalDepth >= MaxlHopitalDepth || lHopitalApplications >= MaxlHopitalApplications || suppresslHopital)
                return null;
            // Held for the whole of the rule and not just for the recursive call below, since
            // asking what the two parts tend to is itself a limit that the rule may be applied
            // to, and counting only the recursion left those two free to nest without a bound.
            lHopitalDepth++;
            try { return ApplylHopitalRuleImpl(expr, x, dest, side); }
            finally { lHopitalDepth--; }
        }

        /// <summary>
        /// Whether a step function in <paramref name="expr"/> jumps in every neighbourhood of the
        /// destination: a floor, a ceil or a round whose argument does not tend to a finite value,
        /// or a sign whose argument has no limit, and so may cross zero all the way in.
        /// </summary>
        /// <remarks>
        /// l'Hopital's rule needs both parts differentiable on a punctured neighbourhood of the
        /// destination. The derivative of floor(x) is 0 provided not x in ZZ, and every
        /// neighbourhood of +oo holds integers, so floor(x) / x at +oo read 0 / 1 and answered 0,
        /// where it is 1. Where the argument tends to a finite value the step is constant on
        /// either side of the destination, and the rule holds there.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2281">#2281</a>
        /// </remarks>
        internal static bool HasAStepThatJumpsNear(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            return expr.Nodes.Any(node => node switch
            {
                Floorf(var argument) => DoesNotSettle(argument),
                Ceilf(var argument) => DoesNotSettle(argument),
                Roundf(var argument) => DoesNotSettle(argument),
                Signumf(var argument) => argument.ContainsNode(x)
                    && EvalAssumingContinuous(argument.Limit(x, dest, side)) is not Real { IsNaN: false },
                _ => false
            });
            bool DoesNotSettle(Entity argument) => argument.ContainsNode(x)
                && EvalAssumingContinuous(argument.Limit(x, dest, side)) is not Real { IsFinite: true };
        }

        private static Entity? ApplylHopitalRuleImpl(Entity expr, Variable x, Entity dest, ApproachFrom side)
        {
            if (expr is not Divf && AsQuotient(expr, x, dest, side) is { } quotient)
                expr = quotient;
            if (expr is Divf(var num, var den))
                if (EvalAssumingContinuous(num.Limit(x, dest, side)) is var numLimit && EvalAssumingContinuous(den.Limit(x, dest, side)) is var denLimit)
                    if (numLimit == 0 && denLimit == 0 ||
                            IsInfiniteNode(numLimit) && IsInfiniteNode(denLimit))
                        if (num is not Number && den is not Number)
                            if (num.ContainsNode(x) && den.ContainsNode(x)
                                && !HasAStepThatJumpsNear(expr, x, dest, side))
                            {
                                // Simplified, because the shape of the quotient is what the
                                // machinery below matches on and differentiation does not
                                // produce it: d/dx ln(x)^2 is 2 * ln(x) * (1 / x), a product
                                // with a quotient inside rather than the quotient
                                // 2 * ln(x) / x, and only the second has a limit here.
                                var applied = (num.Differentiate(x) / den.Differentiate(x)).Simplify();
                                // The domain condition simplification leaves behind is what the
                                // derivative of a logarithm or a root carries, and a Providedf is
                                // not a continuous node, so the quotient would be turned away
                                // unread. Limits already treat the expression as continuous, as
                                // SimplifyAndComputeLimitToInfinity does with the same shape.
                                while (applied is Providedf(var body, _)) applied = body;
                                // A derivative the library cannot compute comes back unevaluated
                                // (https://github.com/asc-community/AngouriMath/issues/958), and
                                // differentiating *that* again only raises its Iterations -- so
                                // every pass produces a quotient the chain below has not seen and
                                // AlreadyBeingDifferentiated never fires. The factorial reaches
                                // here and ran to 200,000 differentiations before this guard.
                                // l'Hopital has nothing to say about a derivative it cannot take.
                                if (applied.Nodes.Any(node => node is Derivativef))
                                    return null;
                                if (AlreadyBeingDifferentiated(applied) || GrewTooMuch(expr, applied))
                                    return null;
                                lHopitalApplications++;
                                MultithreadingFunctional.ExitIfCancelled();
                                lHopitalChain!.Add(applied);
                                try
                                {
                                    if (ComputeLimit(applied, x, dest, side) is { } resLim)
                                        return resLim;
                                }
                                finally { lHopitalChain.RemoveAt(lHopitalChain.Count - 1); }
                            }
            return null;
        }

        /// <summary>
        /// How deep one limit may go into rewriting itself. Each of the rewrites below hands
        /// back another limit to take, and that one is entitled to be rewritten in turn, so
        /// without a bound the work would multiply.
        /// </summary>
        private const int MaxRewriteDepth = 2;

        [ThreadStatic] private static int rewriteDepth;

        /// <summary>
        /// The readings that need the expression rewritten before any solver can see anything
        /// in it. Every one of them costs an expansion or a simplification of the whole
        /// expression, which is why this sits here rather than in the descent that visits each
        /// of its parts, and why it is reached only once it is settled that the expression as
        /// written has no answer.
        /// </summary>
        private static Entity? SolveByRewriting(Entity expr, Variable x, Entity dest)
        {
            if (rewriteDepth >= MaxRewriteDepth)
                return null;
            // -oo is +oo with -x written for x, the same substitution the solvers are handed.
            // Reading the growth of a root off x^d depends on it: x^d is positive in the one
            // direction only.
            if (dest.Evaled is Real { IsNegative: true })
                expr = expr.Substitute(x, -x);
            // Everything below reads the expression on its way to +oo, which after the
            // substitution is where it is going -- while the approach in scope is still the one
            // the limit was asked for. A sign read against -oo of an expression written in -x is
            // the opposite sign: the simplifier took -x and 1 - x for positive, gathered
            // ln(-x) + ln(1 - x) into one logarithm, and lim x->-oo ln(x) + ln(x + 1) - ln(x^2 + x)
            // came back 0 where it is 2 pi i.
            // https://github.com/asc-community/AngouriMath/issues/2022
            var outerApproach = EnterApproach(x, Real.PositiveInfinity);
            try { return SolveByRewritingTowardsInfinity(expr, x); }
            finally { LeaveApproach(outerApproach); }
        }

        /// <summary>
        /// <see cref="SolveByRewriting"/> once the destination is +oo.
        /// </summary>
        private static Entity? SolveByRewritingTowardsInfinity(Entity expr, Variable x)
        {
            var toInfinity = Real.PositiveInfinity;
            var simplified = expr.Simplify();
            if (simplified is Providedf(var body, _))
                simplified = body;

            rewriteDepth++;
            try
            {
                // Simplification can hand back an expression of another kind altogether, and
                // which parts a limit is broken into is decided by that kind:
                // sqrt(x^2 + 1) / sqrt(x^2 + 3x) is broken up as a quotient, while what
                // simplifying gives is the single root sqrt((x^2 + 1) / (x^2 + 3x)), whose
                // argument can be read straight off.
                if (simplified.GetType() != expr.GetType()
                    && Settled(simplified.ComputeLimitDivideEtImpera(x, toInfinity, ApproachFrom.Left)) is { } byShape)
                    return byShape;

                if (ExtractRadicalGrowth(simplified, x) is { } extracted
                    && Settled(extracted.ComputeLimitDivideEtImpera(x, toInfinity, ApproachFrom.Left)) is { } byGrowth)
                    return byGrowth;

                // A factorial's logarithm has no reading anywhere else -- every route out of
                // ln(f) differentiates f, and a factorial's derivative wants the digamma
                // function. SolveAsIndeterminatePower reaches the ones sitting under a
                // vanishing exponent; this reaches the rest, which is why ln(x!)/x had no
                // answer while ((x!)/x^x)^(1/x) did.
                // https://github.com/asc-community/AngouriMath/issues/765
                if (StirlingRewritten(simplified, x, toInfinity) is { } byStirling
                    && Settled(ComputeLimit(byStirling, x, toInfinity)) is { } byFactorial)
                    return byFactorial;

                // And the factorials that are not under a logarithm at all, where what may be
                // dropped is a relative error rather than an additive one. Second, because the
                // logarithm above is the cheaper reading and the sounder one -- an additive
                // error that vanishes needs less of the expression to be true of it than a
                // relative one that tends to 1.
                if (!substitutingFactorial
                    && StirlingByItsOwnLogarithm(simplified, x, toInfinity) is { } logarithm)
                {
                    substitutingFactorial = true;
                    try
                    {
                        if (Settled(ComputeLimit(logarithm, x, toInfinity)) is { } exponent)
                            return MathS.e.Pow(exponent).InnerSimplified;
                    }
                    finally { substitutingFactorial = false; }
                }

                return Settled(SolveAsDifferenceOfInfinities(simplified, x));
            }
            finally { rewriteDepth--; }

            // Rewriting brings domain conditions with it -- dividing by x^d is only the same
            // expression where x is not zero -- and a limit is taken of a continuous
            // expression regardless of the points where it is undefined, as the solvers
            // themselves do with the same shape.
            static Entity? Settled(Entity? limit)
            {
                while (limit is Providedf(var inner, _)) limit = inner;
                return limit is null || limit.Evaled == MathS.NaN ? null : limit;
            }
        }

        /// <summary>
        /// The whole expression with every root of a polynomial rewritten so that its growth is
        /// a factor of its own -- <c>sqrt(x^2 + x)</c> becomes <c>x * sqrt(1 + 1/x)</c> -- or
        /// <see langword="null"/> if it has no such root. Every solver reads a polynomial or a
        /// substitution of +oo, and neither can say anything about a root of a sum, so
        /// <c>lim x -&gt; +oo sqrt(x^2 + x) / x</c> was left unevaluated while the rewritten
        /// <c>sqrt(1 + 1/x)</c> is settled by substitution alone.
        /// </summary>
        /// <remarks>
        /// <c>P = x^d * (P / x^d)</c>, and <c>(uv)^r = u^r v^r</c> needs <c>u</c> to be positive,
        /// which <c>x^d</c> is for every x past some point on the way to +oo. That is the only
        /// direction this is used in: a destination of -oo has already been turned into +oo by
        /// substituting -x for x before any of this runs.
        /// </remarks>
        private static Entity? ExtractRadicalGrowth(Entity expr, Variable x)
        {
            if (!expr.Nodes.Any(IsRootOfASum))
                return null;
            var extracted = expr.Replace(RewriteRoot);
            return extracted == expr ? null : extracted.Simplify();

            bool IsRootOfASum(Entity node)
                => node is Powf(Sumf or Minusf, Number.Rational and not Number.Integer);

            Entity RewriteRoot(Entity node)
            {
                if (!IsRootOfASum(node)
                    || node is not Powf(var @base, var power)
                    || !TreeAnalyzer.TryGetPolynomial(@base, x, out var monomials))
                    return node;
                var degree = monomials.Keys.Aggregate(EInteger.Zero, EInteger.Max);
                if (degree.CompareTo(EInteger.One) < 0)
                    return node;
                var growth = MathS.Pow(x, Number.Integer.Create(degree));
                return MathS.Pow(x, Number.Integer.Create(degree) * power) * MathS.Pow((@base / growth).Simplify(), power);
            }
        }

        /// <summary>
        /// How many times a single limit may be broken down as a difference of two divergent
        /// parts. The conjugate below turns one difference into another, and every part of it
        /// is a limit in its own right, so without a bound the work would multiply.
        /// </summary>
        private const int MaxDifferenceDepth = 2;

        [ThreadStatic] private static int differenceDepth;

        /// <summary>
        /// Which infinity an already computed limit is, or 0 if it is finite or not a number.
        /// </summary>
        private static int InfiniteSign(Entity? limit)
            => limit?.Evaled is Real { IsFinite: false, IsNaN: false } real ? (real.IsNegative ? -1 : 1) : 0;

        /// <summary>
        /// oo - oo, which says nothing on its own: whichever of the two grows faster decides
        /// the answer, and if neither does the difference can still be finite. Two of the
        /// standard readings are covered -- one part outgrowing the other, and the conjugate
        /// for a difference containing a root.
        /// </summary>
        private static Entity? SolveAsDifferenceOfInfinities(Entity expr, Variable x)
        {
            if (differenceDepth >= MaxDifferenceDepth || expr is not (Sumf or Minusf))
                return null;
            var terms = Sumf.LinearChildren(expr).ToArray();
            if (terms.Length != 2)
                return null;
            var dest = Real.PositiveInfinity;
            differenceDepth++;
            try
            {
                var (firstSign, secondSign) =
                    (InfiniteSign(ComputeLimit(terms[0], x, dest)), InfiniteSign(ComputeLimit(terms[1], x, dest)));
                if (firstSign == 0 || secondSign != -firstSign)
                    return null;
                // Written as minuend - subtrahend with both parts tending to +oo, so that the
                // reading below does not have to carry the signs around with it.
                var (minuend, subtrahend) = firstSign > 0
                    ? (terms[0], -terms[1])
                    : (terms[1], -terms[0]);

                // The faster growing part decides the answer whenever there is one, which is
                // what the ratio of the two says: lim x -> +oo e^x - x is +oo because x / e^x
                // tends to 0, and -oo the other way round. This settles nothing when the two
                // grow alike, and the ratio then tends to 1 rather than to 0 or to infinity.
                if (ComputeLimit((subtrahend / minuend).Simplify(), x, dest) is { } ratio)
                {
                    if (ratio.Evaled == Number.Integer.Zero)
                        return Real.PositiveInfinity;
                    if (InfiniteSign(ratio) > 0)
                        return Real.NegativeInfinity;
                }

                // a - b = (a^2 - b^2) / (a + b), an identity wherever a + b is not zero, which
                // it is not on the way to +oo. It is only an improvement when squaring removes
                // a root, and then the leading terms cancel in the numerator and what is left
                // is an ordinary quotient: sqrt(x^2 + x) - x becomes x / (sqrt(x^2 + x) + x).
                if (!ContainsRadical(minuend, x) && !ContainsRadical(subtrahend, x))
                    return null;
                MultithreadingFunctional.ExitIfCancelled();
                var conjugate = ((minuend * minuend - subtrahend * subtrahend) / (minuend + subtrahend)).Simplify();
                if (conjugate == expr)
                    return null;
                // Without the roots the numerator is an ordinary polynomial and the denominator
                // is what it was, so the quotient either falls to the solvers directly or it is
                // no better than what it replaced. Differentiating it repeatedly is a long way
                // round to the same nothing, and it is most of the cost here.
                suppresslHopital = true;
                try { return ComputeLimit(conjugate, x, dest); }
                finally { suppresslHopital = false; }
            }
            finally { differenceDepth--; }
        }

        private static bool ContainsRadical(Entity expr, Variable x)
            => expr.Nodes.Any(node =>
                node is Powf(var @base, Number.Rational and not Number.Integer) && @base.ContainsNode(x));

        private static Entity ApplyTrivialTransformations(Entity expr, Variable x, Entity dest, Func<Entity, Entity, Entity> transformation)
            => expr switch
            {
                Sumf(var a, var b)
                    when ComputeLimit(a, x, dest) is { } aLim && ComputeLimit(b, x, dest) is { } bLim &&
                        IsFiniteNode(aLim.Evaled) && IsFiniteNode(bLim.Evaled)
                        => transformation(a, aLim) + transformation(b, bLim),
                Minusf(var a, var b)
                    when ComputeLimit(a, x, dest) is { } aLim && ComputeLimit(b, x, dest) is { } bLim &&
                        IsFiniteNode(aLim.Evaled) && IsFiniteNode(bLim.Evaled)
                        => transformation(a, aLim) - transformation(b, bLim),
                Mulf(var a, var b)
                    when ComputeLimit(a, x, dest) is { } aLim && ComputeLimit(b, x, dest) is { } bLim &&
                        IsFiniteNode(aLim.Evaled) && IsFiniteNode(bLim.Evaled)
                        => transformation(a, aLim) * transformation(b, bLim),
                _ => expr
            };

        /// <summary>
        /// A tangent or a cotangent written as the quotient it is. Kept apart from
        /// <see cref="TrivialTrigonometricReplacement"/>, which runs in front of the first
        /// remarkable limit, because that limit matches a quotient and this rewrite would turn
        /// the quotient a tangent sits under into a product.
        /// </summary>
        private static Entity AsSineOverCosine(Entity expr, Variable x)
            => expr switch
            {
                Tanf(var arg) when arg.ContainsNode(x) => MathS.Sin(arg) / MathS.Cos(arg),
                Cotanf(var arg) when arg.ContainsNode(x) => MathS.Cos(arg) / MathS.Sin(arg),
                _ => expr
            };

        private static Entity TrivialTrigonometricReplacement(Entity expr, Variable x)
            => expr switch
            {
                Secantf(var arg) when arg.ContainsNode(x) => 1 / MathS.Cos(arg),
                Cosecantf(var arg) when arg.ContainsNode(x) => 1 / MathS.Sin(arg),
                _ => expr
            };

    }
}
