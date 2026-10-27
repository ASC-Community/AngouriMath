//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Convenience;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// Integrates indefinitely the given expression over the `x` variable, if can.
        /// May return an unresolved <see cref="Integralf"/> node.
        /// </summary>
        /// <param name="x">Over which variable to integrate</param>
        /// <returns>
        /// An integrated expression. It might remain the same or be transformed into nodes with no integrals.
        /// </returns>
        /// <remarks>
        /// The antiderivative is inner-simplified before it is returned, as the definite
        /// overload below has always done. Without it the answer kept branches whose guard
        /// was a comparison of two literals -- <c>1/sqrt(x^2 - 1)</c> came back as a
        /// three-branch piecewise on <c>1 * 1 ^ 2</c>, two of them dead -- and a caller that
        /// inspects the answer rather than evaluating it saw all three. `Piecewise` already
        /// drops a branch whose guard is decidably false; nothing was asking it to.
        /// https://github.com/asc-community/AngouriMath/issues/772
        /// </remarks>
        public Entity Integrate(Variable x) =>
            Transformation.Integration(x).Apply(this).Output is { } antiderivative
            ? antiderivative + (antiderivative.VarsAndConsts.Contains("C") ? Variable.CreateUnique(antiderivative, "C") : "C")
            : new Integralf(this, x, null);
        /// <summary>
        /// Integrates definitely the given expression over the `x` variable, if can.
        /// May return an unresolved <see cref="Integralf"/> node.
        /// </summary>
        /// <param name="x">Over which variable to integrate</param>
        /// <param name="from">The lower bound for integrating</param>
        /// <param name="to">The upper bound for integrating</param>
        /// <returns>
        /// An integrated expression. It might remain the same or be transformed into nodes with no integrals.
        /// </returns>
        public Entity Integrate(Variable x, Entity from, Entity to) =>
            // An integrand that can jump -- a piecewise whose conditions mention x, a floor or a
            // ceiling of x -- never goes through an antiderivative between two bounds: the rules
            // integrate it with the case or the step taken for a constant, which is right
            // between two jumps and wrong across one. (x - floor(x))^2 from 0 to 4 came back as
            // 0 that way, where it is 4/3, and the tent map over [0, 1] as 1, where it is 1/2.
            // It is split at the jumps instead, and where that cannot be done it is left as
            // written. See BreakpointIntegration.
            Functions.Algebra.BreakpointIntegration.HasABreak(this, x)
            ? Functions.Algebra.BreakpointIntegration.Split(this, x, from, to) ?? new Integralf(this, x, (from, to))
            : Transformation.Integration(x).Apply(this).Output is { } antiderivative
                ? AcrossTheRange(antiderivative, x, from, to) ?? new Integralf(this, x, (from, to))
                : new Integralf(this, x, (from, to));

        /// <summary>
        /// <c>F(b) - F(a)</c> through <paramref name="antiderivative"/>, taken piece by piece
        /// where it breaks between the bounds, or <see langword="null"/> where a piece's limit at
        /// a break is not decided.
        /// </summary>
        /// <remarks>
        /// At each break the piece below takes its limit from the left and the piece above from
        /// the right, so a jump in the antiderivative is not counted and a pole is: <c>1/x^2</c>
        /// over <c>[-1, 1]</c> is <c>+oo</c>, and <c>1/x</c> over the same range, whose pieces are
        /// <c>-oo</c> and <c>+oo</c>, has no value. Where the bounds are not numbers, or the breaks
        /// cannot all be listed, the antiderivative is taken at the bounds alone, as it was.
        /// https://github.com/asc-community/AngouriMath/issues/1508
        /// </remarks>
        private static Entity? AcrossTheRange(Entity antiderivative, Variable x, Entity from, Entity to)
        {
            if (from.Evaled is not Number.Real { IsNaN: false } lower || to.Evaled is not Number.Real { IsNaN: false } upper
                || lower == upper
                || (lower < upper
                    ? AntiderivativeBreaks.Inside(antiderivative, x, lower, upper)
                    : AntiderivativeBreaks.Inside(antiderivative, x, upper, lower)) is not { Count: > 0 } breaks)
                return AtTheBound(antiderivative, x, to, from) is { } atTheTop && AtTheBound(antiderivative, x, from, to) is { } atTheBottom
                    ? atTheTop - atTheBottom
                    : null;
            if (lower > upper)
                breaks.Reverse();
            // Not across a break of an antiderivative in the incomplete gamma function: see AtTheBound.
            if (HoldsAnIncompleteGammaFunction(antiderivative) || AtTheBound(antiderivative, x, from, to) is not { } atTheStart)
                return null;
            var total = -atTheStart;
            foreach (var (at, _) in breaks)
            {
                // Approached from the side the range comes from, then left on the other.
                var (arriving, leaving) = lower < upper ? (ApproachFrom.Left, ApproachFrom.Right) : (ApproachFrom.Right, ApproachFrom.Left);
                var before = antiderivative.Limit(x, at, arriving);
                var after = antiderivative.Limit(x, at, leaving);
                if (before.Nodes.Any(node => node is Limitf) || after.Nodes.Any(node => node is Limitf))
                    return null;
                total += before - after;
            }
            return AtTheBound(antiderivative, x, to, from) is { } atTheEnd ? total + atTheEnd : null;
        }

        private static bool HoldsAnIncompleteGammaFunction(Entity expr)
        {
            foreach (var node in expr.Nodes)
                if (node is IncompleteGammaf)
                    return true;
            return false;
        }

        /// <summary>
        /// The antiderivative's value at one bound of a definite integral: its value there, and
        /// where that is undefined, its limit as the bound is approached from inside the range,
        /// which is what an improper integral is.
        /// </summary>
        /// <remarks>
        /// <c>-(x + 1) e^(-x)</c> at <c>+oo</c> is <c>-oo * 0</c>, which is <c>NaN</c>, so the integral
        /// of <c>x e^(-x)</c> from 0 to <c>+oo</c> was <c>NaN</c> where it is 1; and <c>x ln(x) - x</c>
        /// at 0 is <c>0 * -oo</c>, so the integral of <c>ln(x)</c> from 0 to 1 was <c>NaN</c> where it
        /// is -1. The limit is taken only where the value is undefined, so a bound the substitution
        /// answered is answered as before. Its side is known at an infinity, and at a finite bound
        /// from the other bound where both have numeric values. Elsewhere, and where the limit is
        /// undefined, the value stays undefined, as it is for <c>sin(x)</c> from 0 to <c>+oo</c>,
        /// whose antiderivative has no limit there. Where the limit is not decided there is no value,
        /// <see langword="null"/>, and the integral is left as written.
        /// </remarks>
        private static Entity? AtTheBound(Entity antiderivative, Variable x, Entity bound, Entity otherBound)
        {
            var value = antiderivative.Substitute(x, bound);
            if (!value.InnerSimplified.IsNaN)
                return value;
            // The bounds' values, so that a bound written as pi or 2 pi is placed as well as 3.
            ApproachFrom? side = (bound.Evaled, otherBound.Evaled) switch
            {
                (Number.Real { EDecimal: var at }, _) when at.IsPositiveInfinity() => ApproachFrom.Left,
                (Number.Real { EDecimal: var at }, _) when at.IsNegativeInfinity() => ApproachFrom.Right,
                (Number.Real at, Number.Real other) when at > other => ApproachFrom.Left,
                (Number.Real at, Number.Real other) when at < other => ApproachFrom.Right,
                _ => null
            };
            if (side is not { } inside)
                return value;
            // An antiderivative in the incomplete gamma function undefined at a bound is left as written rather than
            // taken to its limit there: x^3 (-x^4)^(-3/4) gamma(3/4, -x^4), the integral of x^2 e^(x^4), is 0 times an
            // infinity at 0, and its limit went through l'Hopital's rule on the whole product, differentiating the function
            // and evaluating Gamma(3/4) at every step, for over a minute. https://github.com/asc-community/AngouriMath/issues/2075
            if (HoldsAnIncompleteGammaFunction(antiderivative))
                return null;
            var limit = LimitAtTheBound(antiderivative, x, bound, inside);
            // A limit that is not decided is no value: the integral is left as written. Undefined would say it does not
            // exist, and the integral of ln(x) erf(x) from 0 to 1 was NaN where it is -0.2613, for want of erf's limit at
            // 0. https://github.com/asc-community/AngouriMath/issues/2081
            if (limit.Nodes.Any(node => node is Limitf))
                return null;
            return limit.IsNaN ? value : limit;
        }

        /// <summary>
        /// The antiderivative's limit at a bound, remembered under the settings it was taken in, as
        /// the cache of answers remembers an antiderivative. A definite integral left as written is evaluated
        /// again by every candidate <c>Simplify</c> tries, and the limit that left it so is the slow part: at 0,
        /// the antiderivative of <c>ln(x) erf(x)</c> is an infinity less an infinity that l'Hopital's rule does
        /// not settle, two seconds of search each time, and the simplification ran past two minutes.
        /// https://github.com/asc-community/AngouriMath/issues/2081
        /// </summary>
        private static Entity LimitAtTheBound(Entity antiderivative, Variable x, Entity bound, ApproachFrom side)
        {
            if (limitsAtBounds is null || limitsAtBoundsUnder is null || !SettingsState.StillHolds(limitsAtBoundsUnder))
            {
                limitsAtBounds = new();
                limitsAtBoundsUnder = SettingsState.Snapshot();
            }
            var into = limitsAtBounds;
            var stamp = limitsAtBoundsUnder;
            var key = (antiderivative, x, bound, side);
            if (into.TryGetValue(key, out var known))
                return known;
            // Within a budget of the limits it asks along the way. Past it the limit is undecided and the integral left
            // as written, which is no wrong answer. The most a decided limit at a bound took in the tests is 957; one
            // spelling of the antiderivative of ln(x) Si(x) took over 38,000 in forty seconds without deciding.
            var limit = Functions.Algebra.LimitFunctional.WithinABudget(LimitsAtABound, () => antiderivative.Limit(x, bound, side));
            if (SettingsState.StillHolds(stamp))
            {
                if (into.Count >= 256)
                    into.Clear();
                into[key] = limit;
            }
            return limit;
        }
        [System.ThreadStatic] private static Dictionary<(Entity, Variable, Entity, ApproachFrom), Entity>? limitsAtBounds;
        /// <summary>How many limits the limit at one bound may ask in all.</summary>
        private const int LimitsAtABound = 2000;
        [System.ThreadStatic] private static object?[]? limitsAtBoundsUnder;

        /// <summary>
        /// Integrates numerically over <paramref name="x"/> between two bounds, without
        /// looking for an antiderivative. Only works for one-variable functions.
        /// </summary>
        /// <param name="x">The variable to integrate over.</param>
        /// <param name="from">The lower bound.</param>
        /// <param name="to">The upper bound.</param>
        /// <param name="stepCount">
        /// How many panels to cut the segment into, each integrated by the five-point Gauss–Legendre rule; more
        /// is more accurate and slower. Neither bound is evaluated, so an integrable singularity at one is
        /// allowed, and one that diverges is NaN. The value has the digits the rule agrees on as the panels are
        /// doubled, at most fifteen, a double's. A singularity inside the segment is not seen.
        /// </param>
        /// <returns>The value of the integral.</returns>
        /// <remarks>
        /// This is the instance method the obsolete <c>MathS.Compute.DefiniteIntegral</c>
        /// said to use and which did not exist — every other member of that group had a
        /// counterpart on <see cref="Entity"/> and this one did not, so removing the group
        /// would have taken numeric definite integration with it.
        /// </remarks>
        public Number.Complex DefiniteIntegral(Variable x, Number.Complex from, Number.Complex to, int stepCount = 100)
            => Integration.IntegrateNumerically(this, x, from, to, stepCount);

        /// <summary>
        /// Integrates numerically over <paramref name="x"/> between two real bounds.
        /// </summary>
        /// <param name="x">The variable to integrate over.</param>
        /// <param name="from">The lower bound.</param>
        /// <param name="to">The upper bound.</param>
        /// <param name="stepCount">
        /// How many panels to cut the segment into, each integrated by the five-point Gauss–Legendre rule; more
        /// is more accurate and slower. Neither bound is evaluated, so an integrable singularity at one is
        /// allowed, and one that diverges is NaN. The value has the digits the rule agrees on as the panels are
        /// doubled, at most fifteen, a double's. A singularity inside the segment is not seen.
        /// </param>
        /// <returns>The value of the integral.</returns>
        public Number.Complex DefiniteIntegral(Variable x, EDecimal from, EDecimal to, int stepCount = 100)
            => Integration.IntegrateNumerically(this, x,
                Number.Complex.Create(from, 0), Number.Complex.Create(to, 0), stepCount);
    }
}

namespace AngouriMath.Functions.Algebra
{
    internal static partial class Integration
    {
        /// <summary>
        /// Brings the two powers of one base in a product together, so that an integrand
        /// which is a power in disguise is recognised as one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This has to happen on every recursive call rather than once on the way in. The
        /// shape is produced by the integrator itself -- distributing a product over a sum
        /// turns <c>sin(x)^4 * (5 - 6*sin(x)^2)</c> into a term <c>sin(x)^4 * (-6) * sin(x)^2</c>
        /// -- so normalising only the caller's input would miss every case the integrator
        /// generates for itself. https://github.com/asc-community/AngouriMath/issues/781
        /// </para>
        /// <para>
        /// Deliberately not followed by <c>InnerSimplified</c>: that rewrites <c>x^(-2)</c>
        /// back into <c>1/x^2</c>, and <see cref="IndefiniteIntegralSolver.SolveAsPolynomialTerm"/>
        /// rewrites a <c>1/x^n</c> it is handed into <c>Pow(x, -n)</c>, so the pair recurse
        /// into each other until the stack runs out. The gathering folds the exponents it
        /// builds and leaves the rest of the tree alone.
        /// </para>
        /// </remarks>
        private static Entity Normalized(Entity expr, Entity.Variable x) =>
            WithItsPiecewiseSumsCombined(expr.Replace(Patterns.GatherPowersOfOneBase).Replace(node => IndefiniteIntegralSolver.GatherSignsAndModuli(node, x))
                // A whole power of a fractional power is the power of the product of the
                // exponents, exact for every base: `((c^2 x^2 + 1)^(-1/2))^2` is
                // `(c^2 x^2 + 1)^(-1)`, and left as a square of a root it was handed to Euler's
                // substitution as a radical beside `(c^2 x^2 + 1)^(-1)`, which answered it
                // wrongly -- the second step of parts on `acsch(c x)^2/x^4` was that. The
                // gathering above joins two powers of one base beside each other and not one
                // inside the other. A fractional inner power only: `((1 + x^2)^(-1))^4` is
                // spelled so by the rules that read it, and flattened it sent Timofeev's
                // `x^3 arctan(x)^2/(1 + x^2)^3` from 0.1 s to 8.
                .Replace(node => node is Entity.Powf(Entity.Powf(var @base, var inner), Entity.Number.Integer whole) && inner is Entity.Number.Rational && inner is not Entity.Number.Integer && @base.ContainsNode(x)
                    ? MathS.Pow(@base, (inner * whole).InnerSimplified)
                    : node)
                // A zeroth power of anything with the variable in it is one but where its base is
                // zero, a point the antiderivative passes through as the generic case does every
                // removable one: `x/(x (a + b x^2 + c x^4))` arrives as `x^0/(a + b x^2 + c x^4)`,
                // and no rule reads `x^0` as a polynomial, so it was declined where
                // `1/(a + b x^2 + c x^4)` is answered.
                .Replace(node => node is Entity.Powf(var zeroth, Entity.Number.Integer { IsZero: true }) && zeroth.ContainsNode(x)
                    ? Entity.Number.Integer.One
                    : node));

        /// <summary>
        /// <paramref name="expr"/> with every sum of piecewises in it combined case by case, as
        /// the evaluator did before an answer's sum of piecewises was left a sum: an integrand
        /// is searched as the one piecewise, and taken apart term by term it was searched once a
        /// term. The sums are written anew, so that no simplification of them is remembered from
        /// outside the scope.
        /// https://github.com/asc-community/AngouriMath/issues/1874
        /// </summary>
        private static Entity WithItsPiecewiseSumsCombined(Entity expr)
        {
            if (!expr.Nodes.Any(node => node is Entity.Sumf or Entity.Minusf && node.DirectChildren.Any(child => child is Entity.Piecewise)))
                return expr;
            var combining = Entity.CombinesEveryPiecewiseSum;
            Entity.CombinesEveryPiecewiseSum = true;
            try
            {
                return expr.Replace(node => node switch
                {
                    Entity.Sumf(var augend, var addend) => new Entity.Sumf(augend, addend),
                    Entity.Minusf(var minuend, var subtrahend) => new Entity.Minusf(minuend, subtrahend),
                    _ => node
                }).InnerSimplified;
            }
            finally
            {
                Entity.CombinesEveryPiecewiseSum = combining;
            }
        }

        /// <summary>
        /// The integrals already answered under the settings in force, so that the same question
        /// is not worked out again.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Why an integral is asked for twice at all.</b> Two layers of it. Within one call the
        /// solvers overlap — substitution, partial fractions, splitting a sum and integration by
        /// parts each decompose the integrand differently and produce pieces that coincide — and
        /// across calls <c>Simplify</c> asks about one integral through every
        /// rewritten candidate it generates. Traced on <c>sin(x)/(x^2 + 1)^2</c>, which has no
        /// elementary antiderivative and so runs the search to exhaustion, that is <b>562
        /// top-level calls for 3 distinct integrands</b>, one of them asked 500 times; on
        /// <c>e^x/(x^2 + 1)^2</c> a single call enters the integrator <b>5330 times for 23</b>.
        /// </para>
        /// <para>
        /// <b>The answers are held across calls and not only within one</b>, which is where the
        /// repetition is: discarding them at the end of each top-level call leaves the 562 to be
        /// paid in full. Measured on those three integrands, keeping them takes a
        /// <c>Simplify</c> from 115, 86 and 51 seconds to about 1.2, 1.5 and 0.2, and a single
        /// cold <c>Integrate</c> of <c>sqrt(tan(x))</c> from 155 ms to 79.
        /// https://github.com/asc-community/AngouriMath/issues/1156
        /// </para>
        /// <para>
        /// <b>Why it is sound to hold them.</b> An answer depends on the ambient settings —
        /// <see cref="MathS.Settings.Codomain"/> decides whether the logarithms carry an
        /// <c>abs</c>, <c>MaxExpansionTermCount</c> bounds the by-parts recursion — and a setting
        /// is scoped to a flow rather than to a thread, so "the settings have not changed" is not
        /// something a per-thread cache can assume. <see cref="SettingsState"/> answers it
        /// exactly, by comparing what every setting reads as rather than by counting changes —
        /// which matters, because the library opens and closes thousands of balanced scopes while
        /// simplifying and a change count is therefore never still.
        /// </para>
        /// <para>
        /// <b>A decline is held with the scope it was made in.</b> Rules answer only the question
        /// asked (<see cref="AnsweringTheQuestionAsked"/>), or it and one or two levels below it
        /// (<see cref="AnsweringTheQuestionAskedOrOneBelow"/>,
        /// <see cref="AnsweringTheQuestionAskedOrTwoBelow"/>), and decline the same integrand
        /// deeper, so a <see langword="null"/> computed at one depth says nothing about a
        /// shallower one. Held without the scope it was served there: <c>sec(x)^3</c>, tried and
        /// declined inside another rule's search, then asked for directly and declined from the
        /// cache in two milliseconds. And held with the top told only from the rest, a decline at
        /// depth three was served at depth two: the search for <c>1/(x^(3/2) (a + c x^4))</c>
        /// declines the root's substitution <c>2/(w^2 (a + c w^8))</c> at depth three and asks it
        /// again at depth two, and the integral was declined. The key carries the depth as far as
        /// a scope reaches, <see cref="ScopeDepth"/>, and a lookup takes a decline only from its
        /// own depth and an answer from any, since an antiderivative that was found is right
        /// wherever it is asked for.
        /// </para>
        /// </remarks>
        [System.ThreadStatic] private static Dictionary<(Entity, Entity.Variable, bool, int), (Entity? Answer, bool ASumOverRootsWould)>? answered;

        /// <summary>The settings <see cref="answered"/> was filled under.</summary>
        [System.ThreadStatic] private static object?[]? answeredUnder;

        /// <summary>
        /// How many answers to hold before starting over. Comfortably above what one call needs
        /// — the worst measured is 23 distinct integrands within a call and 3 across the
        /// candidates of one <c>Simplify</c> — and low enough that a process integrating
        /// different things all day does not accumulate them.
        /// </summary>
        private const int MostAnswersKept = 4096;

        /// <summary>
        /// How deep the recursive descent may go before it declines to go further.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Without this the descent has **no bound at all**, and a stack overflow is not an
        /// exception a caller can handle: it takes the process down, so anything the process had
        /// not finished is lost. Found by <c>work/intbench</c> against Rubi's independent test
        /// suites, where the run died with <c>SIGABRT</c> partway through; the trace was thousands
        /// of frames alternating <see cref="ComputeIndefiniteIntegral"/> with
        /// <see cref="IndefiniteIntegralSolver.SolveBySubstitution"/>.
        /// https://github.com/asc-community/AngouriMath/issues/1232
        /// </para>
        /// <para>
        /// <b>Why the memo above does not already stop it, which is the part worth stating.</b>
        /// <c>SolveBySubstitution</c> names its new variable with
        /// <c>Variable.CreateUnique</c>, so every level integrates with respect to a *fresh*
        /// variable. The key <c>(expr, x, integrateByParts)</c> therefore differs at every level
        /// even when the level is the same problem renamed, and <see cref="answered"/> can never
        /// fire on a cycle. Neither could a set of shapes already visited, for the same reason —
        /// the shapes are alpha-equivalent rather than equal. A depth bound does not care what
        /// the levels are called.
        /// </para>
        /// <para>
        /// <b>The number, which is measured rather than picked.</b> Instrumenting the descent over
        /// twenty-three integrands chosen from the hard end of the corpus — the ones that take
        /// substitutions, by-parts chains and partial fractions — the deepest any *answered*
        /// integral reaches is <b>13</b>, for <c>x^5*cosh(x)</c>. Next are <c>x^2*sqrt(5-x^2)</c>
        /// at 8 and <c>e^(x^(1/3))</c> at 7; everything else sits at 4 or less. 32 is therefore
        /// about two and a half times the deepest real descent seen.
        /// </para>
        /// <para>
        /// <b>Why not far more, since a bound only has to stop the overflow.</b> Because this
        /// descent branches: the remark on <see cref="answered"/> above records a single call
        /// entering the integrator 5,330 times for 23 distinct integrands. Depth that is never
        /// legitimately used is still searched before it is abandoned, so a bound of 64 stopped
        /// the crash and left the run crawling. A bound has to be tight enough to be a bound.
        /// </para>
        /// <para>
        /// Declining is a legitimate answer here and a wrong one is not: an unevaluated
        /// <c>integral(...)</c> says "I could not settle this", which is true, where an aborted
        /// process says nothing at all.
        /// </para>
        /// </remarks>
        private const int DeepestDescent = 32;

        /// <summary>How deep the current descent is. Per thread, like <see cref="answered"/>.</summary>
        [System.ThreadStatic] private static int descentDepth;

        /// <summary>
        /// Whether the integrand now being solved is the one the caller asked for, rather than
        /// one a rule produced on the way to answering something else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For a rule that is the right answer to a question and a distraction as an offer. A
        /// rule which answers a sub-integral that used to come back <see langword="null"/> does
        /// not only add its own work: it lets the search that asked stop failing and carry on,
        /// which is where the cost turns up — on integrands the rule never fires on.
        /// </para>
        /// <para>
        /// Measured on the secant power reduction: <c>sec(x)^6*tan(x)^3</c> is declined in 637 ms
        /// without it and does not return in 400 s with it, while the rule itself fires exactly
        /// twice and terminates both times. The work is not the rule's; the continuation is.
        /// https://github.com/asc-community/AngouriMath/issues/1265
        /// </para>
        /// <para>
        /// One is the top: the depth is incremented before the solvers run, so a rule consulted
        /// about the caller's own integrand sees exactly one.
        /// </para>
        /// </remarks>
        internal static bool AnsweringTheQuestionAsked => descentDepth == 1;

        /// <summary>
        /// Whether the integrator is running on this thread: for a <c>Simplify</c> candidate that
        /// is wanted by its callers and not by the integrator's rules, which read the shape of what
        /// <c>Simplify</c> returns.
        /// </summary>
        internal static bool IsIntegrating => descentDepth > 0;

        /// <summary>
        /// The question asked or one step below it: the remainder by parts leaves, the
        /// integrand a substitution makes. For a rewriting rule that lands on the chain and is
        /// wanted there and no deeper, where the same rewriting fed a search that did not return.
        /// </summary>
        internal static bool AnsweringTheQuestionAskedOrOneBelow => descentDepth <= 2;

        /// <summary>
        /// The question asked or one of the two steps below it: the remainder of a step of
        /// parts on a power of an inverse function, and the remainder of the step on that.
        /// </summary>
        internal static bool AnsweringTheQuestionAskedOrTwoBelow => descentDepth <= 3;

        /// <summary>
        /// The depth as far as a scope tells one depth from another: 1 for the question asked, 2
        /// and 3 for the levels <see cref="AnsweringTheQuestionAskedOrOneBelow"/> and
        /// <see cref="AnsweringTheQuestionAskedOrTwoBelow"/> reach, and 4 for every level past
        /// them, where every scoped rule declines alike.
        /// </summary>
        private static int ScopeDepth => System.Math.Min(descentDepth, DeepestScope);

        /// <summary>The deepest <see cref="ScopeDepth"/>.</summary>
        private const int DeepestScope = 4;

        /// <summary>
        /// <paramref name="expr"/> integrated as a question in its own right rather than as a
        /// step in the search for the current one: the term of a sum asked at the top is asked at
        /// the top.
        /// </summary>
        /// <remarks>
        /// Linearity splits the integrand before anything else, and every term it produced was
        /// one level down — where the five scoped rules decline. So <c>(x^3 - 1)/(2 + x^3)^(1/3)</c>
        /// was declined although each of its two terms is a binomial differential the rule
        /// answers when asked directly. A term of a top-level sum is strictly smaller than the
        /// sum and is not a continuation of any rule's search, which is what the scope exists to
        /// stop; taken at the top it costs nothing the scope was measured to save. Only from the
        /// top: one level down the terms stay one level down, as before.
        /// </remarks>
        internal static Entity? ComputeAsAQuestionOfItsOwn(Entity expr, Entity.Variable x, bool integrateByParts)
        {
            if (!AnsweringTheQuestionAsked)
                return ComputeIndefiniteIntegral(expr, x, integrateByParts);
            // Past the entry check on purpose: a depth of zero there means a fresh question,
            // and empties the set of integrals in progress and forgets that the descent was
            // truncated. This is not a fresh question -- the integrand this one was made from
            // is still open above it, and its place in that set is what stops the search below
            // from reaching it again and following it round.
            descentDepth--;
            try
            {
                return ComputeIndefiniteIntegralGuarded(expr, x, integrateByParts);
            }
            finally
            {
                descentDepth++;
            }
        }

        /// <summary>
        /// <paramref name="expr"/> integrated as the same question as the one being answered:
        /// the integrand with a constant factor taken off it. <c>b f(x)</c> and <c>f(x)</c> are
        /// one question, and a rule scoped to the question asked or one below it -- a power of
        /// a quotient written apart -- was never consulted about the remainder by parts left
        /// from <c>asech(a + b x)/x^2</c>, because taking its <c>b</c> out, and then a <c>1</c>,
        /// had carried it three levels down before any rule saw it. Past the entry check for
        /// the reason <see cref="ComputeAsAQuestionOfItsOwn"/> gives.
        /// </summary>
        internal static Entity? ComputeAsTheSameQuestion(Entity expr, Entity.Variable x, bool integrateByParts)
        {
            if (descentDepth == 0)
                return ComputeIndefiniteIntegral(expr, x, integrateByParts);
            descentDepth--;
            try
            {
                return ComputeIndefiniteIntegralGuarded(expr, x, integrateByParts);
            }
            finally
            {
                descentDepth++;
            }
        }

        /// <summary>
        /// Whether anything in the current top-level call gave up on <see cref="DeepestDescent"/>
        /// rather than on the mathematics. A <c>null</c> produced that way must not be cached as
        /// "this cannot be integrated", because the same key may well be answerable when it is
        /// not reached from so deep.
        /// </summary>
        [System.ThreadStatic] private static bool descentTruncated;

        /// <summary>
        /// Marks what is being worked out as declined for the scope it was asked in rather than
        /// for its mathematics, as running out of descent is, so that the decline is not
        /// remembered: the same question asked where that scope does not bind may be answered.
        /// </summary>
        internal static void DeclinedForItsScope() => descentTruncated = true;

        /// <summary>
        /// The integrals this thread is part-way through, so that asking for one again while it is
        /// still being worked out is recognised as a cycle rather than followed round again.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Keyed on the integrand with its variable renamed to <see cref="CycleVariable"/>, which
        /// is the whole point. <see cref="IndefiniteIntegralSolver.SolveBySubstitution"/> names
        /// each new variable with <c>Variable.CreateUnique</c>, so a level and the level it came
        /// from are alpha-equivalent rather than equal, and a set keyed on the integrand as
        /// written would never see the same entry twice. Neither does <see cref="answered"/>, and
        /// for the same reason — which is why the memo could not stop this.
        /// </para>
        /// <para>
        /// This is what makes <see cref="DeepestDescent"/> a backstop instead of the mechanism.
        /// The bound alone stops the process dying, and it is not enough on its own: the descent
        /// branches, so 32 levels of a cycle is still an enormous search, and one Rubi integrand
        /// took over ten minutes under the bound where <c>v2.4.0</c> passed the whole section it
        /// is in within a minute. Declining at the first repeat costs nothing and answers at once.
        /// </para>
        /// </remarks>
        [System.ThreadStatic] private static HashSet<(Entity, bool)>? inProgress;

        /// <summary>The name every in-progress integrand's variable is rewritten to.</summary>
        [ConstantField] private static readonly Entity.Variable CycleVariable = Entity.Variable.CreateVariableOrConstant("__integration_cycle_var");

        /// <summary>Does not add the constant of integration because this is called recursively.</summary>
        internal static Entity? ComputeIndefiniteIntegral(Entity expr, Entity.Variable x, bool integrateByParts = true)
        {
            if (descentDepth >= DeepestDescent)
            {
                descentTruncated = true;
                return null;
            }
            if (descentDepth != 0)
                return ComputeIndefiniteIntegralGuarded(expr, x, integrateByParts);
            descentTruncated = false;
            inProgress?.Clear();
            ASumOverRootsWouldAnswer = false;
            var answer = ComputeIndefiniteIntegralGuarded(expr, x, integrateByParts);
            if (answer is not null || !ASumOverRootsWouldAnswer || SumsOverRootsAllowed)
                return answer;
            // Once more with sums over roots, and with a memo of its own, since the first pass
            // remembered every one of those rational functions as declined. Last, and only where
            // nothing else answered: asked with the rest, a sum over roots answered each of
            // Jeffrey's three terms, where the whole is one arctangent the rational integrator
            // writes when it is asked the whole.
            // https://github.com/asc-community/AngouriMath/issues/1285
            var (memo, stamp) = (answered, answeredUnder);
            SumsOverRootsAllowed = true;
            answered = null;
            descentTruncated = false;
            inProgress?.Clear();
            try
            {
                answer = ComputeIndefiniteIntegralGuarded(expr, x, integrateByParts);
            }
            finally
            {
                SumsOverRootsAllowed = false;
                (answered, answeredUnder) = (memo, stamp);
            }
            // In place of the decline the first pass remembered, which a question asked again
            // would otherwise be answered with, without the rule ever being reached to say that a
            // second pass could answer it.
            if (answer is not null && memo is not null && stamp is not null && SettingsState.StillHolds(stamp))
                memo[(Normalized(expr, x), x, integrateByParts, 1)] = (answer, false);
            return answer;
        }

        /// <summary>
        /// Whether the rational integrator may answer with a sum over the roots of a factor of the
        /// denominator: only in the second pass of a question nothing else answered.
        /// </summary>
        [System.ThreadStatic] internal static bool SumsOverRootsAllowed;

        /// <summary>
        /// Set where the rational integrator declined only because the residues lie in a field of
        /// degree above two, which a sum over roots would have written.
        /// </summary>
        internal static bool ASumOverRootsWouldAnswer
        {
            get => aSumOverRootsWouldAnswer;
            set
            {
                if (value)
                    sumsOverRootsWanted++;
                aSumOverRootsWouldAnswer = value;
            }
        }

        [System.ThreadStatic] private static bool aSumOverRootsWouldAnswer;

        /// <summary>
        /// How many times <see cref="ASumOverRootsWouldAnswer"/> was set, which is how a computation
        /// tells whether it set it itself: the flag is one per question asked, and already set by
        /// whatever was asked before.
        /// </summary>
        [System.ThreadStatic] private static int sumsOverRootsWanted;

        /// <summary>
        /// <see cref="ComputeIndefiniteIntegral"/> past its entry check: the cycle guard, the
        /// depth, and the memo.
        /// </summary>
        private static Entity? ComputeIndefiniteIntegralGuarded(Entity expr, Entity.Variable x, bool integrateByParts)
        {
            // Renamed so that the same integral under two different variable names is one entry.
            var cycleKey = (expr.Substitute(x, CycleVariable), integrateByParts);
            var open = inProgress ??= new HashSet<(Entity, bool)>();
            if (!open.Add(cycleKey))
            {
                // Already on the stack: following it again cannot reach an answer this call has
                // not already tried, so declining is both correct and immediate.
                descentTruncated = true;
                return null;
            }

            descentDepth++;
            try
            {
                return ComputeIndefiniteIntegralBounded(expr, x, integrateByParts);
            }
            finally
            {
                open.Remove(cycleKey);
                descentDepth--;
            }
        }

        private static Entity? ComputeIndefiniteIntegralBounded(Entity expr, Entity.Variable x, bool integrateByParts)
        {
            expr = Normalized(expr, x);

            if (answered is null || answeredUnder is null || !SettingsState.StillHolds(answeredUnder))
            {
                // A fresh dictionary rather than Clear(), because this flow may have inherited
                // the reference from the one that started it, and emptying it in place would
                // empty that one's too.
                answered = new();
                answeredUnder = SettingsState.Snapshot();
            }

            // Held as locals over the recursive call, which reaches this method again and may
            // replace both fields on the way. Writing the answer into whatever the fields hold
            // afterwards would put it in a dictionary stamped with different settings.
            var into = answered;
            var stamp = answeredUnder;

            var depth = ScopeDepth;
            var key = (expr, x, integrateByParts, depth);
            if (into.TryGetValue(key, out var already))
            {
                // A decline remembered from a first pass that a sum over roots would have answered says
                // so again: the memo outlives the question, and without this the one asked next on the
                // thread was declined where the second pass answers it. Integrating
                // `(1 + x^4)/(1 - x^4 + x^8)` and then `(1 + (1 + sqrt(3)) x^4)/(1 - x^4 + x^8)` declined
                // the second, which answered alone.
                if (already.Answer is null && already.ASumOverRootsWould)
                    ASumOverRootsWouldAnswer = true;
                return already.Answer;
            }
            for (var other = 1; other <= DeepestScope; other++)
                if (other != depth && into.TryGetValue((expr, x, integrateByParts, other), out var elsewhere) && elsewhere.Answer is not null)
                    return elsewhere.Answer;

            // Whether the descent ran out *inside this computation*, which is the only truncation
            // that says anything about this key. The flag is one per thread, so it is cleared
            // for the duration and what it held before is put back, joined with what happened
            // here: a decline reached after some unrelated branch above was cut short is still a
            // decline, and it used to go uncached along with everything else asked after that
            // branch -- a term of a top-level sum re-did its declined sub-integrals for that.
            var truncatedBefore = descentTruncated;
            descentTruncated = false;
            var wantedBefore = sumsOverRootsWanted;
            Entity? computed;
            bool truncatedHere;
            try
            {
                computed = ComputeIndefiniteIntegralUncached(expr, x, integrateByParts);
            }
            finally
            {
                truncatedHere = descentTruncated;
                descentTruncated = truncatedBefore || truncatedHere;
            }

            // Only kept if those settings still hold. Working the answer out runs simplification,
            // which opens scopes of its own; one still open means this answer was computed under
            // settings the stamp does not describe.
            // A null that came of running out of descent, rather than of the mathematics, is not
            // an answer about this integrand and must not be remembered as one — the same key
            // reached from less deep may well be answerable. A non-null answer is kept whatever
            // happened elsewhere, since an antiderivative that was found is correct regardless of
            // how deep the search that found it went.
            if (SettingsState.StillHolds(stamp) && (computed is not null || !truncatedHere))
            {
                // Emptied rather than grown without end. Nothing here expires on its own — the
                // settings holding still is the whole condition for keeping an answer — so a
                // long-lived process integrating a stream of different expressions would hold
                // every one of them for ever. What this is for is being asked the same question
                // again, which needs the recent answers and not all of them.
                if (into.Count >= MostAnswersKept)
                    into.Clear();
                into[key] = (computed, computed is null && sumsOverRootsWanted != wantedBefore);
            }
            return computed;
        }

        private static Entity? ComputeIndefiniteIntegralUncached(Entity expr, Entity.Variable x, bool integrateByParts)
        {
            if (!expr.ContainsNode(x)) return expr * x; // base case, handle here
            // A condition with no x in it holds for every x or for none, so the integral of `f provided C`
            // is the integral of f, provided C. Inner simplification writes one at the top of a question a
            // rule hands on -- `d^0` is `1 provided not d = 0` -- and no rule below reads it: AJ's kept
            // spelling of `1/((c + d x)(a + b (c + d x)^3))` lost eight rows to that. The same question,
            // so that a rule scoped to the one asked still reads it. Only the question asked: below it, the
            // partial fractions of Rubi's 1.2.3.2 `(d f + e f x)/(a + b (d + e x)^2 + c (d + e x)^4)` hand on a
            // piece with `provided not e = 0` that answered took 0.9 GB, for the answer the rest gives anyway.
            if (expr is Entity.Providedf(var underTheCondition, var condition) && !condition.ContainsNode(x) && AnsweringTheQuestionAsked)
                return ComputeAsTheSameQuestion(underTheCondition, x, integrateByParts) is { } underIt
                    ? new Entity.Providedf(underIt, condition)
                    : null;
            if ((IntegralPatterns.TryStandardIntegrals(expr, x)) is { } answer) return answer;
            // The flag has to be handed on. Every one of these recurses, and a solver that
            // dropped it re-enabled integration by parts one level below the call that
            // switched it off -- which is a cycle, since by parts calls back into here.
            // `x * ln(x)` went round it until the stack ran out.
            // A sum free of the variable that comes to a number multiplied out, as that number,
            // before anything reads a coefficient or an exponent in it: `2 + 2 a - 2 (1 + a)`.
            if ((answer = IndefiniteIntegralSolver.SolveByFoldingConstantSumsToTheirNumbers(expr, x, integrateByParts)) is { }) return answer;
            // And a squared sine beside the squared cosine of its argument, the one they add up to:
            // `e^(cos(x)^2 + sin(x)^2)` was declined, written with an x its value does not hold.
            if ((answer = IndefiniteIntegralSolver.SolveByFoldingAPythagoreanPair(expr, x, integrateByParts)) is { }) return answer;
            // A logarithm of nested powers of one linear, `ln(c (d (e + f x)^p)^q)`, named as `p q ln(e + f x)`
            // plus a constant, before the rules that declined its cube beside a quadratic after twelve gigabytes.
            if ((answer = IndefiniteIntegralSolver.SolveByNamingALogarithmOfNestedPowersOfALinear(expr, x, integrateByParts)) is { }) return answer;
            // An arctangent over a power of d (1 +- i c x), under x = tan(u)/c, where 1 + i tan(u) is
            // e^(i u)/cos(u): the rules below ran out of time on `(a + b atan(c x))^2/(d + i c d x)^2`.
            if ((answer = IndefiniteIntegralSolver.SolveAnArctangentOverAPowerOfOnePlusICXUnderTheTangent(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of i times an inverse trigonometric function written algebraically, at the
            // question asked, before the rules that spent the budget: `e^(3 i atan(a + b x))/x^2` ran out of
            // time where, rewritten, it is answered in half a second. It is asked again in its place below.
            // An exact derivative among them first, closed: `e^(i n arctan(a x)) x^2 (c + a^2 c x^2)^(-1 - n^2/2)`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExactDerivativeOfAnExponentialOfAnArctangent(expr, x)) is { }) return answer;
            if (AnsweringTheQuestionAsked && (answer = IndefiniteIntegralSolver.SolveByWritingAnExponentialOfAnInverseAlgebraically(expr, x, integrateByParts)) is { }) return answer;
            // The same for a symbolic multiple of the arctangent, beside powers of its quadratic.
            if (AnsweringTheQuestionAsked && (answer = IndefiniteIntegralSolver.SolveAnExponentialOfASymbolicMultipleOfAnArctangentAsPowersOfTheLinears(expr, x, integrateByParts)) is { }) return answer;
            // A polynomial over a symbolic quartic that splits into two quadratics in a hidden variable,
            // palindromic or even after a shift: the rules below spent seconds declining both.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverASymbolicQuarticThatSplitsIntoTwoQuadratics(expr, x, integrateByParts)) is { }) return answer;
            // A symbolic power of a sine, cosine, secant or cosecant times `alpha + beta f^2`, where it is
            // the derivative of `cos sin^(m + 1)`, `sin cos^(m + 1)`, `tan sec^m` or `cot csc^m`.
            if ((answer = IndefiniteIntegralSolver.SolveASymbolicPowerOfATrigonometricFunctionAsTheDerivativeOfAProduct(expr, x)) is { }) return answer;
            // And numbers not whole for the exponents, two apart, by the reduction from the highest
            // down: split, `1/csc(u)^(3/2) - sqrt(csc(u))/3` is two elliptic integrals.
            if ((answer = IndefiniteIntegralSolver.SolveFractionalPowersOfATrigonometricFunctionTwoApartByTheReduction(expr, x)) is { }) return answer;
            // And the derivative of a sine raised from nothing times powers of the cosine and sums in it,
            // or of a cosine beside the sine, alone or beside x by parts: the half angle declined
            // `(a + a cos(u))^(3/2)/cos(u)^(5/4)`, and parts integrated `x (b + a sin(x))/(a + b sin(x))^2`'s
            // derivative into a function of tan(x/2) and ran out of time.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheDerivativeOfASineRaisedFromNothingBesidePowersOfTheCosine(expr, x, integrateByParts)) is { }) return answer;
            // And its hyperbolic twin, where the functions are exponentials: the derivative of a sum of
            // them over another, read off the integrand over the other's square, alone or beside x by
            // parts. The split and the exponential substitution answered `(b - a sinh(x))/(a + b sinh(x))^2`
            // with a piecewise on a discriminant, and parts beside x ran past the budget integrating it.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheDerivativeOfAQuotientOverASumOfExponentials(expr, x, integrateByParts)) is { }) return answer;
            // And x times a rational function of an exponential of a logarithm, read off the integrand by
            // the Euler derivative: `(1 - b^2 n^2) sech(u) + 2 b^2 n^2 sech(u)^3` for `u = a + b ln(c x^n)`.
            if ((answer = IndefiniteIntegralSolver.SolveAsXTimesARationalFunctionOfAnExponentialOfALogarithm(expr, x)) is { }) return answer;
            // A cosine beside a function of the sine with a power off the rationals in it, in the sine:
            // the substitution search declined `cos(x)/(sin(x) + sin(x)^sqrt(2))`.
            if ((answer = IndefiniteIntegralSolver.SolveACosineBesideAFunctionOfTheSineWithAnIrrationalPowerInIt(expr, x, integrateByParts)) is { }) return answer;
            // A power of a sine, cosine or hyperbolic one over a binomial in a power of it, over the
            // binomial's roots, before the rules that spent the budget on `sinh(y)^4/(a + b sinh(y)^3)`.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOverABinomialInAPowerOfASineOverItsRoots(expr, x)) is { }) return answer;
            // x^j T^alpha Q^beta for a quotient of linears T and a multiple Q of their product, where the
            // antiderivative is T^alpha Q^(beta + 1) times a polynomial of degree j - 1, before the rewrite
            // below hands `e^(6 artanh(a x)) x^2/(c - a^2 c x^2)^19` to partial fractions of degree 38.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXBesidePowersOfAQuotientOfLinearsAndTheirProductAlgebraically(expr, x)) is { }) return answer;
            // An exponential of a whole multiple of an inverse hyperbolic tangent, written algebraically,
            // before the rules that timed out on `e^(artanh(a x)) x^2/(c - a^2 c x^2)^2`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialOfAnInverseHyperbolicTangentAlgebraically(expr, x)) is { }) return answer;
            // A power of a cosine over a power of k (1 +- sin), cancelled: `cos^2/((e + f x)(a + a sin))`.
            if ((answer = IndefiniteIntegralSolver.SolveACosinePowerOverABinomialInTheSineByCancelling(expr, x)) is { }) return answer;
            // A function of an inverse function of c + d x beside powers of multiples of it, under
            // u = c + d x, before the rules that timed out on `(c e + d e x)^3 (a + b arsinh(c + d x))^2`.
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfAnInverseOfALinearBesideItsPowersUnderTheLinear(expr, x)) is { }) return answer;
            // A power of x times an inverse tangent and the logarithm of its quadratic, by parts with the
            // two remainders that have no antiderivative apiece paired, before the rules that timed out.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXTimesAnArctangentAndTheLogarithmOfItsQuadratic(expr, x)) is { }) return answer;
            // A polynomial times 1, an inverse hyperbolic tangent or cotangent of c x, the logarithm of
            // 1 - c^2 x^2 and their product, in closed form, before the rules that timed out on
            // `x^3 (a + b artanh(c x))(d + e ln(1 - c^2 x^2))`.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialTimesAnInverseHyperbolicTangentAndTheLogarithmOfItsQuadratic(expr, x)) is { }) return answer;
            // A rational function over the quadratic of an inverse tangent beside it, split, before the
            // rules that timed out on `x^4 atan(a x)/(c + a^2 c x^2)`.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOverTheQuadraticOfAnArctangentBesideIt(expr, x)) is { }) return answer;
            // A power of x times a power of a sum holding an inverse function of x^n, under t = x^n,
            // before the rules that timed out on `x^3 (a + b atanh(c x^2))^2`.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXTimesAFunctionOfAnotherUnderThatPower(expr, x)) is { }) return answer;
            // An even power of a cosine over a whole power of a binomial in the sine, or the other way
            // round, by the reduction formulas in the power of the binomial, before the half-angle ran out
            // of time on `cos(c + d x)^8/(a + b sin(c + d x))^8` with symbols for coefficients.
            if ((answer = IndefiniteIntegralSolver.SolveAnEvenPowerOfACosineOverAPowerOfABinomialInTheSineByReduction(expr, x)) is { }) return answer;
            // A power of a secant over one of a cos(y) + b sin(y), the two together odd, in powers of
            // a + b tan(y), before the half-angle ran out of time on `sec(c + d x)^4/(a cos + b sin)^3`.
            if ((answer = IndefiniteIntegralSolver.SolveASecantPowerOverAPowerOfACosineAndASineInPowersOfABinomialInTheTangent(expr, x)) is { }) return answer;
            // An odd power of a sine over a power of a + b tan(y)^2 under u = cos(y), a - b named, before
            // the substitution search ran out of time on `sin(e + f x)^3/(a + b tan(e + f x)^2)^3`.
            if ((answer = IndefiniteIntegralSolver.SolveAnOddPowerOfASineOverAPowerOfABinomialInTheSquaredTangentUnderTheCosine(expr, x)) is { }) return answer;
            // An odd power of a tangent beside a function of sin(y)^2 under v = sin(y)^2, before the
            // secant's substitution took eight seconds on `tan(e + f x)^5/(a + b sin(e + f x)^2)^(5/2)`.
            if ((answer = IndefiniteIntegralSolver.SolveAnOddPowerOfATangentBesideAFunctionOfTheSquaredSineUnderIt(expr, x)) is { }) return answer;
            // From the twelfth power, an even power of a tangent over a power of k (1 +- sec), cancelled:
            // the rules after it ran out of time on `tan(c + d x)^12/(a + a sec(c + d x))^3`.
            if ((answer = IndefiniteIntegralSolver.SolveAnEvenPowerOfATangentOverAPowerOfOnePlusASecantByCancelling(expr, x)) is { }) return answer;
            // A root of k (1 +- sin) or of k (1 +- cos), or of it over something in the function, beside
            // another root, under u = sin or u = cos: the half angle reads only roots of the first kind,
            // and `sqrt((1 - cos(x))/(a - cos(x)))` ran out of time.
            if ((answer = IndefiniteIntegralSolver.SolveARootOfOnePlusASineBesideAnotherRootUnderTheSine(expr, x)) is { }) return answer;
            // A polynomial in a sine or a cosine over a whole power of a binomial in it, in powers of the
            // binomial, before the half-angle wrote 30 million characters for `(A + B cos + C cos^2)/(a + b cos)^4`.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialInASineOverAPowerOfABinomialInItInPowersOfTheBinomial(expr, x)) is { }) return answer;
            // A rational function times the logarithm of a nested product of powers of linears, by parts,
            // its logarithm's derivative read off as written: `ln(e (f (a + b x)^p (c + d x)^q)^r)`.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionTimesALogarithmOfPowersOfLinears(expr, x, integrateByParts)) is { }) return answer;
            // x^m (d + e x^2)^p (a + b F(c x)) with m + 2p + 3 = 0 and F an inverse trigonometric function, by
            // parts against v = x^(m + 1) (d + e x^2)^(p + 1)/(d (m + 1)), before by parts against the
            // integrator's piecewise v timed out on `x^3 (a + b asin(c x))/(d + e x^2)^3`.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXOverAPowerOfAQuadraticTimesAnInverseTrigonometricFunctionByParts(expr, x, integrateByParts)) is { }) return answer;
            // A function of an inverse of something not linear times its derivative up to a factor
            // constant though written with x: `atan(c x/sqrt(a - c^2 x^2))^2/sqrt(d - c^2 d x^2/a)`. Early,
            // since it fires only where that factor holds x, and the rules before it spent 28 seconds on
            // `atan(e x/sqrt(-a e^2/b - e^2 x^2))/sqrt(a + b x^2)` declining.
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfAnInverseTimesItsDerivativeUpToAConstant(expr, x, integrateByParts)) is { }) return answer;
            // The derivative of a function over a power of a logarithm, an inverse trigonometric
            // function, or a sum with a logarithm in it, whose terms are not elementary apart: early,
            // since it reads only a power of one below the bar, and by parts on either term asked the
            // other's question again.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheDerivativeOfAFunctionOverAPowerOfALogarithmOrAnInverse(expr, x)) is { }) return answer;
            // A whole power of a linear in the inverse sine, cosine or hyperbolic cosine of
            // `c + d x^2` with `c^2 = 1`, by its recurrence, which closes: by parts it went round.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAnInverseFunctionOfAQuadraticByTheRecurrence(expr, x)) is { }) return answer;
            // An algebraic function of 1/x alone with a root in it, under x = 1/u, before the linears
            // are written once: `1/((a + b/x)^(5/2) (c + d/x)^2)` ran past the budget that way.
            if ((answer = IndefiniteIntegralSolver.SolveAnAlgebraicFunctionOfTheReciprocalOfX(expr, x, integrateByParts)) is { }) return answer;
            // A product of powers of quotients of linears with one linear in two of them, written
            // over each linear once with the constant that takes in front. First, since it answers
            // the question asked only, and the polynomial term below writes `1/u` as `u^(-1)` and
            // asks that a level down: `1/(e^atanh(a x) (c - c/(a x))^(3/2))` reached it nowhere.
            // Two linears to one power that is not whole, as one quadratic to that power, before each
            // linear is written once: `(2 + 3x)^4/((1 - 2x)^(5/2) (3 + 5x)^(5/2))` took forty-five
            // seconds there, and over `(3 - x - 10x^2)^(5/2)` it takes half of one.
            // A linear below the cube root of a quadratic, in closed form where Rubi has one.
            if ((answer = IndefiniteIntegralSolver.SolveALinearBelowACubeRootOfAQuadraticInClosedForm(expr, x)) is { }) return answer;
            // And over the cube root of a quadratic beside another about the same axis, in the
            // variable that takes the linear terms away: Rubi's 1.2.1.6 pseudo-elliptic 232 is 231 there.
            if ((answer = IndefiniteIntegralSolver.SolveOverTheCubeRootOfAQuadraticBesideAnotherAboutOneAxis(expr, x, integrateByParts)) is { }) return answer;
            // And beside the cube roots of two linears whose product is a quadratic, with the two roots
            // written back for the root of the product.
            if ((answer = IndefiniteIntegralSolver.SolveALinearBesideCubeRootsOfTwoLinearsAsOneOfAQuadratic(expr, x)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveTwoLinearsToOnePowerAsOneQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // A linear and a quadratic to one power as one binomial, `(1 + x) (1 - x + x^2)` as
            // `1 + x^3`, which the rules for a power of a binomial answer.
            if ((answer = IndefiniteIntegralSolver.SolveALinearAndAQuadraticToOnePowerAsOneBinomial(expr, x, integrateByParts)) is { }) return answer;
            // Cube roots of two linears beside a whole power of a third, under the quotient of the
            // roots with the coefficients named, before the polynomial term takes it a level down to
            // the substitution there, whose rational function ran past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveCubeRootsOfTwoLinearsBesideAWholePowerOfAThird(expr, x, integrateByParts)) is { }) return answer;
            // And a root of a linear over a second power or higher of `b x + c x^2`, under the root,
            // split over the quadratic's two linears: the substitution there multiplies it out.
            if ((answer = IndefiniteIntegralSolver.SolveAnOddHalfPowerOfALinearOverAPowerOfAQuadraticWithARootAtZero(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByWritingEachLinearOnce(expr, x, integrateByParts)) is { }) return answer;
            // The same with a symbol for an exponent, where the product is one power that is not
            // whole beside whole ones: `e^atanh(a x) x^m sqrt(1 - a^2 x^2)` is `K x^m (a x + 1)`, and ran
            // out of time behind it.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingEachLinearOnceUnderASymbolicPower(expr, x, integrateByParts)) is { }) return answer;
            // A rational function with numbers for coefficients over a denominator past the twelfth
            // degree, by Hermite's reduction before anything searches: nothing below answered one.
            if ((answer = IndefiniteIntegralSolver.SolveAHighDegreeRationalFunctionByHermite(expr, x)) is { }) return answer;
            // And a polynomial over high whole powers of two linears with a symbol in them, under
            // t = L1/L2, where the partial fractions ran past the budget: `x^2/((1 - a x)^16 (1 + a x)^11)`.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverHighWholePowersOfTwoLinears(expr, x)) is { }) return answer;
            // A whole power of the square of a polynomial written out, written over its root: the rules
            // for a power of a binomial read `(a + b x^2)^4` where `(a^2 + 2 a b x^2 + b^2 x^4)^2` ran
            // past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveAWholePowerOfASquareOverItsRoot(expr, x, integrateByParts)) is { }) return answer;
            // A power of a linear beside the derivative of a quadratic over a whole power of it, by
            // parts once, before anything takes the symbolic quadratic apart.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfALinearBesideTheDerivativeOfAQuadraticOverItsPower(expr, x, integrateByParts)) is { }) return answer;
            // A power of a linear that is not whole, times a linear, over a whole power of a quadratic,
            // the power of the quadratic lowered one at a time.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfALinearOverAPowerOfAQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // A power of x below -1 over a high whole power of a symbolic binomial, the power of x
            // raised to -1 or more: before the next, which reads x as a linear beside the quadratic,
            // and the partial fractions, which ran out the budget on `1/(x^6 (a + b x^2)^10)`.
            if ((answer = IndefiniteIntegralSolver.SolveANegativePowerOfXOverAWholePowerOfABinomialByRaisingThePowerOfX(expr, x, integrateByParts)) is { }) return answer;
            // A rational function over a whole power of a symbolic quadratic, the power lowered one
            // at a time, before the partial fractions take the quadratic apart over its roots.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOverAPowerOfASymbolicQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // A power of x over a sextic that is a sum of two cubes, split over the cube roots of
            // unity into three quadratics before the partial fractions try the symbolic sextic.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXOverASumOfTwoCubes(expr, x, integrateByParts)) is { }) return answer;
            // A power of an even quadratic over a binomial in it, split over the binomial's roots.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAnEvenQuadraticOverABinomialInIt(expr, x, integrateByParts)) is { }) return answer;
            // A root or a power of a product beside a power of one of its factors, the product
            // split: `(a + b x^2)^(5/2)/sqrt(a^2 - b^2 x^4)` is `K (a + b x^2)^2/sqrt(a - b x^2)`.
            // Early, since what reads the product whole is slower and longer: Rubi's 1.2.1.2:2218,
            // `1/((d + e x)^4 (a d e + (c d^2 + a e^2) x + c d e x^2)^(3/2))`, took 27 seconds to
            // reach it and its split form takes a quarter of one.
            if ((answer = IndefiniteIntegralSolver.SolveBySplittingARootOfAProductBesideARootOfAFactor(expr, x, integrateByParts)) is { }) return answer;
            // A symbolic trinomial in x^2 below the bar beside a root of a quadratic, over its roots
            // in x^2, before anything reads the trinomial whole: asked of it, the routes below ran
            // past a minute on Rubi's 1.2.2.4 `sqrt(d + e x^2)/(x^2 (a + b x^2 + c x^4))`.
            if ((answer = IndefiniteIntegralSolver.SolveATrinomialInTheSquareBesideTheRootOfAQuadratic(expr, x)) is { }) return answer;
            // And the root of such a trinomial over a binomial in x^4 matched to it, c d + a e = 0, at
            // a c < 0, under u = x/sqrt(a + b x^2 + c x^4): Welz's `sqrt(1 - x^4)/(1 + x^4)`.
            if ((answer = IndefiniteIntegralSolver.SolveTheRootOfATrinomialInTheSquareOverABinomialInTheFourthPower(expr, x)) is { }) return answer;
            // A linear under a power inside a sum, written as the variable: `x^3/(a + b (c + d x)^3)`.
            // Before the polynomial term, which takes the leading coefficient out of a power of
            // the sum and writes what is left out in powers of x, where the linear is gone; and
            // before the substitution search, which reads one function at a time.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAFunctionOfOneShiftedLinear(expr, x, integrateByParts)) is { }) return answer;
            // And a transcendental function of one shifted linear alone, for the same reason: the
            // polynomial term writes `(c/d + x)^19` beside `F^(a + b/(c + d x)^3)`, two spellings.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingATranscendentalFunctionOfOneShiftedLinear(expr, x, integrateByParts)) is { }) return answer;
            // Powers of two linears beside a negative power of a logarithm of their quotient, which in
            // the quotient are a power of it beside the logarithm alone, onto the exponential integral:
            // the polynomial term takes the slope out of the linear and asks it again, past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveByTheQuotientOfTheLogarithmsLinearsOntoTheExponentialIntegral(expr, x)) is { }) return answer;
            // A polynomial beside error functions of one shifted linear, written in it: by parts in
            // x leaves a Gaussian of the shifted linear beside a polynomial, which nothing reads.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialBesideErrorFunctionsOfAShiftedLinear(expr, x, integrateByParts)) is { }) return answer;
            // And beside a symbolic power of a logarithm of one, or an exponential of a power of one, the same way.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnExponentAsAPowerOfAShiftedLinear(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialBesideATranscendentalPowerOfAShiftedLinear(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXBesideATranscendentalPowerOfABinomial(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveAPowerBesideALaurentPolynomialInAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // A half-odd power of a ± a sin(y) beside powers of other linears in the sine, by the half
            // angle, before the polynomial term: beneath it the substitution search ran out of time.
            if ((answer = IndefiniteIntegralSolver.SolveAHalfOddPowerOfOnePlusMinusASineBesidePowersOfLinearsInIt(expr, x, integrateByParts)) is { }) return answer;
            // And a half-odd power of the cosine itself beside sqrt(a - a cos(y)) or its reciprocal,
            // rational in sin(y)/(sqrt(a - a cos(y)) sqrt(cos(y))), the sine's the same: the polynomial
            // term hands the reciprocal of the product down a level, and the sign of the complement
            // wrote it there with sgn(sqrt(cos(y)) - 1), owing cos(y) >= 0.
            if ((answer = IndefiniteIntegralSolver.SolveAHalfOddPowerOfACosineBesideTheRootOfAMinusACosine(expr, x)) is { }) return answer;
            // The same half-odd power of two linears beside a squared quadratic, as one power of their
            // product: `1/((a + b x + c x^2)^2 sqrt(1 - d x) sqrt(1 + d x))`, before the polynomial term.
            if ((answer = IndefiniteIntegralSolver.SolveTwoEqualHalfOddPowersOfLinearsAsOneBesideAPowerOfAQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // A power below -1 of a cube root of a binomial beside a high power of x and another binomial,
            // divided by the root here, where it is answered: a level down it ran past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveByDividingByAHighPowerOfARootBesideAnotherBinomialFirst(expr, x)) is { }) return answer;
            // A power of x that is not whole beside binomials in x^n, split over them in w = x^n first,
            // before the polynomial term makes them monic and the substitution under the root takes
            // `u^10/((a + b u^4) (c + d u^4)^2)` apart over fourth roots: `x^(9/2)/((a + b x^2) (c + d x^2)^2)`.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXBesideBinomialsInAPowerOfItSplitInThatPower(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveAsPolynomialTerm(expr, x, integrateByParts)) is { }) return answer;
            // A product of powers of the variable with a power of a constant multiple of it
            // among them, `(c x)^m x^n`, by the power rule with the written power kept as
            // it is. Beside the polynomial term, since that is what it is.
            if ((answer = IndefiniteIntegralSolver.SolveAProductOfPowersOfTheVariable(expr, x)) is { }) return answer;
            // A power of x times a power of its logarithm, by the closed reduction: exact,
            // and by parts n times where the exponent is a symbol was not taken.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerTimesAPowerOfTheLogarithm(expr, x)) is { }) return answer;
            // And a half-odd power of it, onto the Gaussian's moments by t = sqrt(F).
            if ((answer = IndefiniteIntegralSolver.SolveAPowerTimesAHalfOddPowerOfTheLogarithm(expr, x)) is { }) return answer;
            // And an exponential of a quadratic in it, onto the Gaussian by t = ln(c x^r).
            if ((answer = IndefiniteIntegralSolver.SolveAPowerTimesAnExponentialOfAQuadraticInALogarithm(expr, x)) is { }) return answer;
            // And a negative whole power of one, onto the exponential integral by t = A + B ln(c x^r).
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOverAPowerOfALogarithm(expr, x)) is { }) return answer;
            // And x^m artanh(A x/sqrt(d + A^2 x^2)), by parts, the logarithm's derivative read as the root it is.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstAnArtanhWhoseDerivativeIsARoot(expr, x, integrateByParts)) is { }) return answer;
            // A power of a quotient of linears beside whole powers of them, which is how e^(n acoth(a x))
            // over a power of c - a^2 c x^2 arrives folded, under t = the quotient.
            if ((answer = IndefiniteIntegralSolver.SolveByTheMoebiusSubstitutionOfAQuotientOfLinears(expr, x)) is { }) return answer;
            // And a power of x times the logarithm of A + B sqrt(Q), by parts, its derivative over the conjugate.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstALogarithmOfARadicalSum(expr, x)) is { }) return answer;
            // A function of tanh(L) or coth(L) alone that is not rational in it, under u = tanh(L).
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfTheHyperbolicTangent(expr, x, integrateByParts)) is { }) return answer;
            // A power of a quotient of linears over a half-odd power of their product, by Liouville's ansatz.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAQuotientOfLinearsOverAHalfOddPowerOfTheirProduct(expr, x)) is { }) return answer;
            // A quotient by a radical sum whose conjugate product is a constant, over the conjugate.
            if ((answer = IndefiniteIntegralSolver.SolveByTheConjugateOfARadicalSumBelowTheBar(expr, x, integrateByParts)) is { }) return answer;
            // By parts against an inverse hyperbolic function beside a non-polynomial, its logarithm's derivative written algebraically.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstAnInverseHyperbolicFunctionAsItsLogarithm(expr, x, integrateByParts)) is { }) return answer;
            // A power of an exponential with a positive base is the exponential of the product,
            // exactly, and only that spelling is one the exponential rules read.
            if ((answer = IndefiniteIntegralSolver.SolveByFlatteningAPowerOfAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // And any other power of an exponential beside a polynomial below the bar, or beside
            // another of the same rate, as a constant multiple of an exponential wherever it is
            // differentiable.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfAnExponentialAsAMultipleOfOne(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of a quadratic in 1/L beside a power of L, onto the Gaussian under u = 1/L.
            if ((answer = IndefiniteIntegralSolver.SolveAGaussianInAReciprocal(expr, x)) is { }) return answer;
            // And of the reciprocal of a linear over another linear, the same substitution's, ahead of
            // its place below: `F^(e + f (a + b x)/(c + d x))/(g + h x)^4` ran past the budget there.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialInTheReciprocalOfALinearOverAnotherLinear(expr, x, integrateByParts)) is { }) return answer;
            // And a trigonometric function of the reciprocal of a linear beside a power of it, under u = 1/L.
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfTheReciprocalOfALinear(expr, x, integrateByParts)) is { }) return answer;
            // And one of `a + b/x` beside any rational function, under `x = 1/u`:
            // `sin(c + d/x)/(e + f x)` is `-sin(c + d u)/(u (e u + f)) du`.
            if ((answer = IndefiniteIntegralSolver.SolveByTheReciprocalOfAFunctionOfTheReciprocal(expr, x, integrateByParts)) is { }) return answer;
            // And a power of the linear alone beside an exponential of a power of its reciprocal, where
            // that is elementary: `F^(a + b/(c + d x)^3)/(c + d x)^13` ran past the budget below.
            if ((answer = IndefiniteIntegralSolver.SolveAnElementaryMomentOfAnExponentialInAPowerOfTheReciprocal(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of a multiple of a logarithm is a power of the argument, which is
            // how every inverse hyperbolic function under an exponential arrives.
            if ((answer = IndefiniteIntegralSolver.SolveByFoldingAnExponentialOfALogarithm(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of a polynomial beside the polynomial's derivative, under u = P,
            // which the substitution search does not reach: it writes the exponential apart.
            if ((answer = IndefiniteIntegralSolver.SolveByTheExponentAsTheVariable(expr, x, integrateByParts)) is { }) return answer;
            // And an exponential of a hyperbolic sine or cosine of a linear beside a function of it
            // over its derivative, under u = twice that sine or cosine, which arrive as exponentials.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialOfAHyperbolicFunctionBesideItsDerivative(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of the tangent beside two roots of linears in it, or one beside a
            // whole power of a + i a tan(z) below the bar, over the linears of 1 + tan^2: in the root
            // of the linears' quotient 1 + tan^2 is a symbolic quartic, and a search past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOfTheTangentBesideRootsOfLinearsInIt(expr, x, integrateByParts)) is { }) return answer;
            // `A + i A tan(z)` is `A e^(i z)/cos(z)`, which beside a polynomial is a shape the
            // closed rules answer, where the imaginary unit in the coefficient is read by none.
            // The same identity beside a power of the secant, where the powers are not whole and add
            // up to a whole number: an exponential over a power of the cosine, in e^(i z). Before the
            // rule below, which writes a power of the identity that is not whole as the power of a
            // product and takes it apart, and so answers (c sec)^(5/2)/(a + i a tan)^(5/2) with the
            // wrong constant wherever the cosine is negative.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAnImaginaryTangentBesideAPowerOfTheSecant(expr, x, integrateByParts)) is { }) return answer;
            // And whatever the powers, in the sum, where they do not add up to a whole number.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfTheSecantBesideAPowerOfAnImaginaryTangentSumInTheSum(expr, x, integrateByParts)) is { }) return answer;
            // And powers of the two conjugate sums, both not whole, as the exponential they make.
            if ((answer = IndefiniteIntegralSolver.SolveAConjugatePairOfImaginaryTangentSums(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of the tangent beside a power of a + i a tan(z), in that sum.
            if ((answer = IndefiniteIntegralSolver.SolveInTheImaginarySumOfAConstantAndATangent(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnImaginaryTangentAsAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // And `A cos(z) + i A sin(z)`, which is `A e^(i z)`, where no rotation is real.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnImaginarySumOfACosineAndASineAsAnExponential(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnImaginarySumOfACosineAndASineOverTheCosine(expr, x, integrateByParts)) is { }) return answer;
            // Exponentials of quadratics, with sines, cosines and sums of them multiplied out:
            // each term is the Gaussian's, where the search would take the product by parts.
            if ((answer = IndefiniteIntegralSolver.SolveAProductOfExponentialsOfQuadratics(expr, x)) is { }) return answer;
            // An exponential of a linear over a power of a linear, onto the exponential integral.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialOfALinearOverAPowerOfALinear(expr, x)) is { }) return answer;
            // And of a quadratic over a power of the linear through its vertex, onto the moments about 0.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialOfAQuadraticOverTheLinearThroughItsVertex(expr, x, integrateByParts)) is { }) return answer;
            // And a sum of exponentials, which is how sinh and cosh arrive, onto Shi and Chi.
            if ((answer = IndefiniteIntegralSolver.SolveAHyperbolicOfALinearOverAPowerOfALinear(expr, x)) is { }) return answer;
            // And either over several linears, split into partial fractions over them first, as the
            // trigonometric rule below splits.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialOverSeveralLinears(expr, x)) is { }) return answer;
            // And exponentials below the bar, where they are whole powers of one exponential and
            // its powers alone are left below: `1/((c + d x)(a + a tanh(e + f x)))`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialBelowTheBarBesideAPowerOfALinear(expr, x)) is { }) return answer;
            // And with sines and cosines beside the exponential, written as exponentials: each term
            // is then the exponential's, with a complex rate.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialTimesATrigonometricOverLinears(expr, x)) is { }) return answer;
            // Sines and cosines of a linear over a power of a linear, onto Si and Ci under u = the
            // linear, where the search would take the quotient by parts without end.
            if ((answer = IndefiniteIntegralSolver.SolveATrigonometricOfALinearOverAPowerOfALinear(expr, x)) is { }) return answer;
            // And an inverse trigonometric function below the bar, which under the substitution that
            // undoes it is the same question; before the search, which does not find it.
            if ((answer = IndefiniteIntegralSolver.SolveAReciprocalOfAnInverseTrigonometricFunction(expr, x, integrateByParts)) is { }) return answer;
            // A fractional power of a perfect square is the power of the modulus, sgn(P) P^(2r).
            if ((answer = IndefiniteIntegralSolver.SolveByTakingARootOfAPerfectSquare(expr, x, integrateByParts)) is { }) return answer;
            // And a square in one trigonometric function, at the top: the modulus of the linear in it.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingARootOfAPerfectSquareInATrigonometricFunction(expr, x, integrateByParts)) is { }) return answer;
            // Four nested roots with a closed form: `sqrt(1 + sqrt(1 - x^2))`,
            // `sqrt(x^2 + sqrt(1 + x^4))/sqrt(1 + x^4)` and the two beside them in Rubi's 1.3.3.
            if ((answer = IndefiniteIntegralSolver.SolveANestedRootByItsClosedForm(expr, x, integrateByParts)) is { }) return answer;
            // And the second of them beside a rational function, over the two conjugate roots it is
            // on the real line: `sqrt(x^2 + sqrt(1 + x^4))/((1 + x) sqrt(1 + x^4))`.
            if ((answer = IndefiniteIntegralSolver.SolveANestedRootOverTheRootInsideItAsTwoConjugateRoots(expr, x, integrateByParts)) is { }) return answer;
            // And any power of a square in any power of x, as the power of its root times a factor
            // constant where the root is not zero: `x^2 (a^2 + 2 a b x^3 + b^2 x^6)^p`.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfASquareAsAPowerOfItsRoot(expr, x, integrateByParts)) is { }) return answer;
            // A linear over a linear beside the root of a symbolic cubic that vanishes midway between
            // the pole and the zero, as one arctangent: Welz's
            // `(a + x)/((x - a) sqrt(a^2 x - (1 + a^2) x^2 + x^3))`. Before x is taken out of the
            // root, after which the search ran out of time on the symbolic ones.
            if ((answer = IndefiniteIntegralSolver.SolveAPseudoEllipticQuotientOverTheRootOfACubicWithARootMidway(expr, x)) is { }) return answer;
            // And a fractional power of a sum whose every term has x in it, the power of x taken out
            // with the factor that is constant where it is not zero: `1/sqrt(a x^2 + b x^5)`.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingAPowerOfXOutOfAFractionalPower(expr, x, integrateByParts)) is { }) return answer;
            // A power of a multiple of a quadratic's derivative beside a power of the quadratic is
            // a binomial in the derivative: `(b d + 2 c d x)^m (a + b x + c x^2)^p`.
            if ((answer = IndefiniteIntegralSolver.SolveByTheDerivativeOfAQuadraticAsTheVariable(expr, x, integrateByParts)) is { }) return answer;
            // x^(n - 1) g(x^n) with a symbolic n is g(u)/n under u = x^n.
            if ((answer = IndefiniteIntegralSolver.SolveByAPowerOfTheVariableTimesAFunctionOfItsPower(expr, x, integrateByParts)) is { }) return answer;
            // And x^(k n - 1) g(x^n) with a whole n >= 2, for a whole power of hyperbolic functions of
            // a linear in x^n, which arrive as exponentials: `x sech(a + b x^2)^7` is
            // `sech(a + b t)^7/2` under t = x^2, where the rules before this spent the budget.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfXTimesAHyperbolicFunctionOfAPowerUnderThatPower(expr, x, integrateByParts)) is { }) return answer;
            // A whole power of a product of a constant and the variable, as the product of
            // the powers, which is how the inverse hyperbolic secant and cosecant arrive.
            if ((answer = IndefiniteIntegralSolver.SolveByDistributingWholePowersOfProducts(expr, x, integrateByParts)) is { }) return answer;
            // And of quotients, which a substitution leaves where it has simplified a sum over
            // the bar: `(c/(a + c u^2))^2` is read by nothing that reads `c^2/(a + c u^2)^2`.
            if ((answer = IndefiniteIntegralSolver.SolveByDistributingWholePowersOfQuotients(expr, x, integrateByParts)) is { }) return answer;
            // The derivative of a power of x over a binomial, over a multiple of the binomial's
            // square plus a power of x: an arctangent of the quotient, which nothing else reads
            // past a quartic -- `(e - 4 f x^3)/(e^2 + 4 d f x^2 + 4 e f x^3 + 4 f^2 x^6)`.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheArctangentOfAPowerOfXOverABinomial(expr, x, integrateByParts)) is { }) return answer;
            // And a fractional or symbolic power of a monomial: `(c x^n)^b` is `c^b x^(n b)`
            // for a positive `c`, on the `x > 0` where a fractional `n` leaves the integrand real,
            // and a constant times `x^(n b)` on each interval for a symbolic one.
            if ((answer = IndefiniteIntegralSolver.SolveByDistributingAPowerOfAMonomial(expr, x, integrateByParts)) is { }) return answer;
            // A power of a constant multiple of the radicand a logarithm holds under a root,
            // written over that radicand: `acosh(c x)` beside `(d - c^2 d x^2)^(5/2)`.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAConstantMultipleOfARadicandOverIt(expr, x, integrateByParts)) is { }) return answer;
            // A power of the variable times a sine or cosine of a logarithm, where two rounds of
            // parts close on the integrand: `int x^m sin(a + b ln(c x^n))` in closed form.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerTimesATrigonometricOfALogarithm(expr, x, integrateByParts)) is { }) return answer;
            // The same with an imaginary coefficient on the logarithm, where the closed form divides
            // by zero: in exponentials, powers of the variable.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingATrigonometricFunctionOfAnImaginaryLogarithmInExponentials(expr, x, integrateByParts)) is { }) return answer;
            // A secant or cosecant power of a logarithm beside the power two below it, where the
            // reduction closes: `sec(a + i ln(c x^n)/(n (p - 2)))^p` is the derivative of a product.
            if ((answer = IndefiniteIntegralSolver.SolveSecantPowersOfALogarithmTwoApartAsTheDerivativeOfAProduct(expr, x)) is { }) return answer;
            // A logarithm of a quotient that cancels with the functions in it as
            // indeterminates, which is how an inverse hyperbolic function of a hyperbolic
            // one arrives: atanh(tanh(u)) is 1/2 ln(e^(2u)) once its quotient is cancelled.
            if ((answer = IndefiniteIntegralSolver.SolveByCancellingInsideALogarithm(expr, x, integrateByParts)) is { }) return answer;
            // A logarithm whose derivative is a constant k is k x + c, and is named so.
            if ((answer = IndefiniteIntegralSolver.SolveByNamingALogarithmLinearInTheVariable(expr, x, integrateByParts)) is { }) return answer;
            // A polynomial times a rational function of exponentials, by parts against the
            // whole rational function, before anything splits the sum: the general parts rule
            // takes each term on its own, and each term's antiderivative keeps a logarithm the
            // sum's does not -- `x tanh(x)^2` was twenty-five seconds of dilogarithms that way.
            // Closed, and declining in a moment where the antiderivative keeps a logarithm.
            if (integrateByParts && (answer = IndefiniteIntegralSolver.SolveAPolynomialTimesARationalFunctionOfAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // An integrand *already written* as a sum is split here, before any search. Every
            // rule below does speculative work on the whole sum first, and on a polynomial that
            // costs everything: the terms of `1 + 2x + ... + 25x^24` integrate in under a
            // millisecond between them, and the sum they add up to took 20 seconds.
            //
            //     degree               4     8     12     16     20     24
            //     was              64ms  384ms  705ms  708ms  2.6s   20.1s
            //     is               69ms   69ms  141ms  213ms  138ms  0.16s
            //
            // **Only a sum, and deliberately not a product with a sum in it.** Expanding one is a
            // rewrite, and a rewrite this early takes answers away from the rules that give
            // better ones: `x * (x^2 + 1)^3` is `(x^2 + 1)^4/8` by substitution and a written-out
            // degree-8 polynomial by expanding, both right and only one worth reading. Three
            // tests pinned exactly that and caught this when the split was put in front of the
            // substitution wholesale. The expanding call stays where it was, below.
            // A whole power below -1 of a linear beside a power that is not whole of another, by the
            // recurrence that raises the whole power to -1: closed, where the substitution for the
            // root makes a rational function over a power of a symbolic quadratic.
            if ((answer = IndefiniteIntegralSolver.SolveANegativePowerOfALinearBesideAPowerOfAnotherByTheRecurrence(expr, x, integrateByParts)) is { }) return answer;
            // A sum of powers of one inverse trigonometric or error function beside cofactors free
            // of it, by parts down the powers from the top: `x^3/((1 + a^2 x^2) atan(a x)^3) -
            // 3 x^2/(2 a atan(a x)^2)` is the derivative of one product, and neither term is
            // elementary on its own. Before the split, which loses it.
            if ((answer = IndefiniteIntegralSolver.SolveASumOfPowersOfAnInverseOrErrorFunctionByPartsDownThePowers(expr, x, integrateByParts)) is { }) return answer;
            // A product of powers times a sum that is the derivative of the product with some
            // of the powers raised by one -- `e^x x^2 ln(x)^2 (3 + (3 + x) ln(x))` -- read off
            // the sum, for symbolic exponents too. Before the split, which loses it. A sine, cosine or
            // exponential in the bracket may be raised from nothing, and what is left beside the
            // derivative may be a constant times it alone, asked: `sinh(Q)/x^2 - b cosh(Q)/x`.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheDerivativeOfAProductOfPowers(expr, x, integrateByParts)) is { }) return answer;
            // And a power of a binomial in x^n beside a power of x, with a symbol in the exponents,
            // where Chebyshev's third case is a whole number at most 0: by Rubi's reduction, which
            // the rule above does not read for a power on its own or past the first step.
            if ((answer = IndefiniteIntegralSolver.SolveABinomialInTheThirdCaseWithASymbolicExponent(expr, x)) is { }) return answer;
            // And a sum of powers of x times a power with a symbol in it of a linear in a root of x,
            // or of its square, the derivative of that power times the linear and powers of the
            // root where the terms' remainders cancel: Rubi's 1.2.3.2 `Q^p/x^2 - K Q^p/x`.
            if ((answer = IndefiniteIntegralSolver.SolveASumOfPowersOfXTimesAPowerOfALinearInARootOfX(expr, x)) is { }) return answer;
            // Powers of two linears whose exponents sum to a whole number below -2, beside a polynomial of
            // low degree, by t = L1/L2: a power of t beside a polynomial. The rule above reads the sum -2.
            if ((answer = IndefiniteIntegralSolver.SolveTwoLinearPowersWhoseExponentsSumToAWholeNumber(expr, x)) is { }) return answer;
            // A rational function of x and of a tower of exponentials and logarithms over it,
            // by the Risch-Norman ansatz: a rational function of the same plus logarithms of
            // the denominator's factors, the coefficients unknown and solved for. Before the
            // split, since Hearn's `e^(x^2)/x + 2x e^(x^2) ln(x) + ...` is a sum whose terms are
            // not elementary apart; at the top only, and only for a tower no closed rule reads.
            if ((answer = IndefiniteIntegralSolver.SolveByRischNormanAnsatz(expr, x)) is { }) return answer;
            // A rational function with numbers beside an odd root of a linear past the square, split over
            // the rationals first where a linear below meets the root at an irrational value:
            // `1/(u^2 (1 - u)^(1/3) (3 + u))`, whose substitution is over the cube root of 4, and which the
            // cancellation below spent five seconds declining.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionBesideAnIrrationalOddRootOfALinearSplitFirst(expr, x, integrateByParts)) is { }) return answer;
            // A quotient of polynomials in x and the functions of x in it, cancelled with those
            // functions as indeterminates and its denominator factored the same way:
            // `(x^2 + 2x sin(x) + sin(x)^2)/(x + sin(x))^2` is `1`, and nothing else reads it.
            // Before the reduction over a linear factor and the split, which expand and take
            // apart what this cancels whole: the remainder by parts leaves from
            // `x asech(a + b x)` is `x^2 (L^3 q^(-1/2) + L^4)/(L^6 (1/L + sqrt(q)))`, which
            // is `x^2/(L^2 sqrt(q))` cancelled, and was five terms over `L^5 (1 + L sqrt(q))`
            // each searched for seconds when the reduction reached it first.
            if ((answer = IndefiniteIntegralSolver.SolveByCancellingWithFunctionsAsIndeterminates(expr, x, integrateByParts)) is { }) return answer;
            // A quotient by the square of a base with a function in it, by parts against the base's
            // reciprocal where the base's derivative makes the rest exact.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstTheReciprocalOfASquaredBase(expr, x)) is { }) return answer;
            // A polynomial over a power of a linear and something that is not a polynomial,
            // the polynomial written in powers of the linear at its root: the part the power
            // divides goes over the rest alone. Before the split, so that the whole polynomial
            // is reduced once rather than each of its terms.
            // A linear over a linear beside the root of a cubic binomial, or a quadratic over a
            // quadratic, at the coefficients where the integral is elementary: Rubi's
            // `(1 + x + sqrt(3))/((1 + x - sqrt(3)) sqrt(1 + x^3))`. Before the reduction over the
            // linear, which writes it as `1` and `2 sqrt(3)` over the linear, neither elementary.
            if ((answer = IndefiniteIntegralSolver.SolveAPseudoEllipticQuotientOverTheRootOfACubicBinomial(expr, x)) is { }) return answer;
            // And a linear over a linear beside the root of an even quartic, where it is a function of
            // the numerator's square over the root: Rubi's `(1 + x - sqrt(3))/((1 + x + sqrt(3)) sqrt(-4 + x^4 + 4 sqrt(3) x^2))`.
            if ((answer = IndefiniteIntegralSolver.SolveALinearOverALinearBesideTheRootOfAnEvenQuarticAsAFunctionOfTheSquare(expr, x)) is { }) return answer;
            // x over a cubic binomial beside the root of another, at the two ratios where that is
            // elementary: Welz's `x/((4 - x^3) sqrt(1 - x^3))`.
            if ((answer = IndefiniteIntegralSolver.SolveXOverACubicBinomialBesideTheRootOfAnother(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByReducingThePolynomialOverALinearFactor(expr, x, integrateByParts)) is { }) return answer;
            // Hyperbolic powers two apart whose coefficients kill the reduction's residual, asked
            // again below, are a sum's before this split too: split, each elliptic term went
            // through the chain alone, and `x/csch(x)^(3/2) + x sqrt(csch(x))/3` was declined
            // here in ten seconds before the rule answered it in a tenth of one. Fractional
            // powers only, which no term of the split answers apart.
            if (expr is Entity.Sumf or Entity.Minusf
                && (answer = IndefiniteIntegralSolver.SolveAPairOfHyperbolicPowersTwoApart(expr, x, integrateByParts)) is { })
                return answer;
            if (expr is Entity.Sumf or Entity.Minusf
                && (answer = IndefiniteIntegralSolver.SolveBySplittingSum(expr, x, integrateByParts)) is { })
                return answer;
            // A constant multiple of D'/D for a sum D below the bar holding a root, decided at
            // sampled points: Hearn's quotient of two roots' derivatives over their sum was a
            // search past the budget for want of the radical algebra the substitution would
            // have needed.
            if ((answer = IndefiniteIntegralSolver.SolveALogarithmicDerivativeOfARadicalSum(expr, x)) is { }) return answer;
            // A negative exponential in a term of a sum below the bar, multiplied out above and below:
            // `(cos(x) + sin(x))/(e^(-x) + sin(x))` is the logarithmic derivative of `1 + e^x sin(x)`.
            if ((answer = IndefiniteIntegralSolver.SolveByClearingANegativeExponentialBelowTheBar(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveLogarithmic(expr, x, integrateByParts)) is { }) return answer;
            // Two or more square roots of polynomials written as one, before the substitution
            // looks for the one subtree it replaces: `x/(sqrt(1 + x^2) sqrt(1 - x^2))` is
            // `x/sqrt(1 - x^4)`, which the substitution answers, and it is what by parts leaves
            // from `arcsin(x)/(1 + x^2)^(3/2)`.
            if ((answer = IndefiniteIntegralSolver.SolveByCombiningRadicals(expr, x, integrateByParts)) is { }) return answer;
            // A polynomial beside the roots of two linears with symbols in them, which the
            // combining above cannot sign, and over a power of one more linear:
            // `(A + B x + C x^2) sqrt(c + d x) sqrt(e + f x)` by undetermined coefficients,
            // where the substitution further down took gigabytes.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverAPowerOfALinearBesideTwoRoots(expr, x)) is { }) return answer;
            // And beside the root of one quadratic: `(d + e x + f x^2) sqrt(a + c x^2)/(g + h x)^4`.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverAPowerOfALinearBesideARootOfAQuadratic(expr, x)) is { }) return answer;
            // A square root of a polynomial with a repeated factor, the factor taken out of
            // the root with its sign: `sqrt((x - 3)^2 (x + 1))` is `sgn(x - 3) (x - 3) sqrt(x + 1)`.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingASquareFactorOutOfARoot(expr, x, integrateByParts)) is { }) return answer;
            // A fractional power of a constant times an even power of a function, the function
            // taken out with its sign: `(a sin(x)^2)^(5/2)` is `a^(5/2) sgn(sin(x)) sin(x)^5`.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingAFunctionOutOfAPowerOfItsEvenPower(expr, x, integrateByParts)) is { }) return answer;
            // And a symbolic root of a power, `(c sin(u)^n)^(1/n)`, the function out and the rest a
            // constant written back.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingAFunctionOutOfASymbolicRootOfItsPower(expr, x, integrateByParts)) is { }) return answer;
            // A sum of two roots below the bar, multiplied above and below by its conjugate:
            // `1/(sqrt(A) - sqrt(B))` is `(sqrt(A) + sqrt(B))/(A - B)`.
            if ((answer = IndefiniteIntegralSolver.SolveByRationalisingASumOfRoots(expr, x, integrateByParts)) is { }) return answer;
            // And a fractional power of a quotient whose denominator is positive for every
            // real x, written as the two powers it is: `((2 + x^2)/x^2)^(7/9)` is nothing any
            // rule reads as written. Beside the rule above, which does the like for the roots
            // it combines; at the top only, since it lands on the chain.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfAQuotientApart(expr, x)) is { }) return answer;
            // A rational function times a whole power of `A + B ln(R)` or `A + B arctan(R)`,
            // R rational: one step of parts, closed, before the substitution search, which
            // spent its budget on Rubi's `(f + g x)(A + B ln(e (a + b x)^2/(c + d x)^2))`.
            // Powers of the two linears of `ln(K (L1/L2)^n)` beside a power of it, by
            // t = L1/L2: a rational function of t beside a logarithm of t, one closed step
            // where the rule below went step by step with the coefficients growing to
            // b^205, and the search after it went past its budget.
            if ((answer = IndefiniteIntegralSolver.SolveByTheQuotientOfTheLogarithmsLinears(expr, x, integrateByParts)) is { }) return answer;
            // And a polynomial over a power of one of those linears, written in powers of it first:
            // `x (a + b atanh(c x))^2/(d + c d x)^3` is two terms the rule above answers, where the
            // rule after it took the whole by parts and the remainder ran past the budget.
            if ((answer = IndefiniteIntegralSolver.SolveByDividingAPolynomialByAPowerOfALinearOfTheLogarithm(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionTimesAPowerOfALogarithm(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of the hyperbolic tangent with a symbol among its
            // coefficients, by u = tanh(y): Rubi's `1/(a + b coth(c + d x)^2)^2` is a symbolic
            // quadratic squared here, and under `u = e^x` a palindromic quartic that took the
            // budget. Before the substitution search, which with a symbolic slope spends the
            // whole of it on `u = c + d x` and never comes back to the chain.
            // Bioche's first two rules for the hyperbolic functions, read back by their
            // exponential spellings: `sinh(x)^3/(a + b cosh(x)^2)` is `(u^2 - 1)/(a + b u^2)`
            // under u = cosh(x), where the substitution search spends the budget on u = e^x.
            // Before the tangent, which takes the half of a plain `e^y` for a symbolic
            // denominator and makes a polynomial of twice the degree of what this answers:
            // `cosh(x)^7/(a + b cosh(x)^2)` and `sech(x)^3/(a + b sinh(x))^2` are a second here
            // and past the budget there.
            if ((answer = IndefiniteIntegralSolver.SolveByBiochesOddHyperbolicSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of e^L that Osborn's rule makes one of a sine or a cosine over a symbolic
            // quadratic in it, asked as that before the tangent: under it Rubi's
            // `sinh(x)^2/(a + b sinh(x) + c sinh(x)^2)` is a quartic with symbols, and its partial
            // fractions ran past the budget, where the sine's rule answers the question in s at once.
            if ((answer = IndefiniteIntegralSolver.SolveAHyperbolicIntegrandAsATrigonometricOne(expr, x, integrateByParts, rationalToo: true, overAQuadraticInTheSine: true)) is { }) return answer;
            // And one of the cosine alone over a sum linear in it, which the tangent made a cube of
            // `1 + t^2` beside a symbolic quadratic: `sech(x)^3/(a + b cosh(x))` in 118,366 characters.
            if ((answer = IndefiniteIntegralSolver.SolveAHyperbolicIntegrandAsATrigonometricOne(expr, x, integrateByParts, rationalToo: true, overALinearInTheCosine: true)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOfTheHyperbolicTangent(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of a sine or a cosine over a quadratic in it with a symbol
            // among the coefficients, split over the quadratic's two roots: under the
            // half-angle it is a quartic in t with a symbol in every coefficient.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOfASineOverASymbolicQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // And of the sine over written powers of linears in it, one with a symbolic root cubed,
            // split in the sine itself: under the half-angle that linear is a symbolic quadratic in
            // t, and the split of its cube took the budget. Before the substitution search, which
            // with a shifted argument asked it again in its own variable and spent the budget.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOfASineOverPowersOfLinearsInIt(expr, x, integrateByParts)) is { }) return answer;
            // And over a binomial in a higher power of it, over the binomial's roots.
            if ((answer = IndefiniteIntegralSolver.SolveAReciprocalOfABinomialInAPowerOfASineOverItsRoots(expr, x)) is { }) return answer;
            // Fractional powers of `a ± a sin(y)` beside anything rational in the sine and
            // cosine, by the half angle at which they are squares: `1 + sin(y)` is `2 sin(u)^2`. Before
            // the substitution search, which spent twenty seconds on the radicals of the sine.
            if ((answer = IndefiniteIntegralSolver.SolveByTheHalfAngleWhereOnePlusASineIsASquare(expr, x, integrateByParts)) is { }) return answer;
            // And a power of `a + b cos(y) + c sin(y)` with `a^2 = b^2 + c^2`, the same square
            // turned by a phase, in closed form: the substitution search spent the budget on it.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfACosineAndASinePlusTheirAmplitude(expr, x)) is { }) return answer;
            // And `a + b cosh(y) + c sinh(y)` with `a^2 = b^2 - c^2`, the hyperbolic one.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAHyperbolicCosineAndSinePlusTheirAmplitude(expr, x)) is { }) return answer;
            // And symbolic powers of `a ± a sin(y)` beside a power of `g cos(y)`, by u = sin(y), where
            // `(1 + u)(1 - u)` is the cosine's square: the half angle wants numeric powers, and the
            // substitution search spent the budget on these. Numeric ones too beside a fractional
            // power of `g cos(y)`, which the half angle does not read.
            if ((answer = IndefiniteIntegralSolver.SolveSymbolicPowersOfOnePlusMinusASineThroughTheSine(expr, x, integrateByParts)) is { }) return answer;
            // And a half-odd power of a +- a sec(y), which is that square over cos(y): by the half-angle
            // tangent, in which the whole is rational beside one root.
            // And `tan(y) tan(2y)` written `sec(2y) - 1` first, so that the rule reads one argument.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingATangentTimesThatOfItsDoubleThroughTheSecant(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByTheHalfAngleTangentBesideAHalfOddPowerOfOnePlusASecant(expr, x, integrateByParts)) is { }) return answer;
            // And beside a half-odd power of `c + d sec(y)`, which the half-angle tangent leaves as a
            // second root: in sin(y/2), where the two powers of cos(y) they bring make a whole one.
            if ((answer = IndefiniteIntegralSolver.SolveTwoHalfOddPowersOfSecantSumsByTheHalfAngleSine(expr, x, integrateByParts)) is { }) return answer;
            // And the secant beside powers of a + a sec(y) and c - c sec(y), whose product is a
            // square of the tangent: in u = a + a sec(y), a symbolic power of it too.
            if ((answer = IndefiniteIntegralSolver.SolveASecantBesidePowersOfItsConjugateSums(expr, x, integrateByParts)) is { }) return answer;
            // And a function of the roots of sec(y) + 1 and sec(y) - 1, in their difference w, of
            // which both and the secant are rational functions: Charlwood's
            // `sqrt(sqrt(1 + sec(x)) - sqrt(sec(x) - 1))`.
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfTheRootsOfTheSecantPlusAndMinusOneInTheirDifference(expr, x, integrateByParts)) is { }) return answer;
            // And an odd power of the tangent beside a function of the secant, in u = sec(y).
            if ((answer = IndefiniteIntegralSolver.SolveAnOddPowerOfTheTangentBesideAFunctionOfTheSecant(expr, x, integrateByParts)) is { }) return answer;
            // And `a ± a cosh(y)` under a fractional power: `2a cosh(y/2)^2`, `-2a sinh(y/2)^2`.
            if ((answer = IndefiniteIntegralSolver.SolveByTheHalfAngleWhereOnePlusAHyperbolicCosineIsASquare(expr, x, integrateByParts)) is { }) return answer;
            // The hyperbolic sine's, off the real line: 1 + i sinh(y) is (cosh(y/2) + i sinh(y/2))^2.
            if ((answer = IndefiniteIntegralSolver.SolveAHalfOddPowerOfOnePlusAnImaginaryHyperbolicSine(expr, x, integrateByParts)) is { }) return answer;
            // And the cosecant's: a ± i a csch(y) is a (w ± i)^2/(w^2 - 1) in w = e^y.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAnImaginaryHyperbolicCosecantSumInTheExponential(expr, x, integrateByParts)) is { }) return answer;
            // And a polynomial in sinh(y) and cosh(y) over a ± i a sinh(y) beside a power of x,
            // divided out, cosh(y)^2 being (1 + i sinh(y))(1 - i sinh(y)).
            if ((answer = IndefiniteIntegralSolver.SolveByDividingOutOnePlusAnImaginaryHyperbolicSine(expr, x, integrateByParts)) is { }) return answer;
            // A symbolic power of one hyperbolic function beside an odd power of the other, under
            // u = the first: Bioche's substitution reads whole and rational powers. The first may be
            // sech(y) or csch(y), and tanh(y) and coth(y) beside it are read as quotients.
            if ((answer = IndefiniteIntegralSolver.SolveASymbolicPowerOfAHyperbolicFunctionBesideAnOddPowerOfTheOther(expr, x, integrateByParts)) is { }) return answer;
            // And cosh(y)^2 taken out of a fractional power of a quotient, `cosh(x) coth(x)` being
            // `cosh(x)^2/sinh(x)`: Bioche's substitution reads the radicands that are polynomials.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingTheHyperbolicCosineSquaredOutOfAPower(expr, x, integrateByParts)) is { }) return answer;
            // And cosh(y)^(2m) over (1 ± i sinh(y))^(2m), or the other way up, cancelled through
            // cosh(y)^2 = (1 + i sinh(y))(1 - i sinh(y)): `1/(sech(x) + i tanh(x))^4` is
            // `(1 - i sinh(x))^2/(1 + i sinh(x))^2`, where the substitutions spent the budget.
            if ((answer = IndefiniteIntegralSolver.SolveByCancellingOnePlusAnImaginaryHyperbolicSineAgainstTheCosineSquared(expr, x, integrateByParts)) is { }) return answer;
            // A quotient of linears as a function's argument, written over its denominator: a
            // constant plus a multiple of the reciprocal of a linear, which the rules read.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAQuotientOfLinearsOverItsDenominator(expr, x, integrateByParts)) is { }) return answer;
            // A cosine and a sine over a power of another such sum, through the denominator, its
            // derivative and a constant, down to the reciprocal of the base: before the substitution
            // search, which reads the quotient term by term.
            if ((answer = IndefiniteIntegralSolver.SolveACosineAndASineOverAPowerOfAnother(expr, x, integrateByParts)) is { }) return answer;
            // Several trigonometric arguments that are multiples of one linear with an offset or a
            // symbolic slope, written in that linear: before the substitution search, which reads
            // each function on its own.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingMultiplesOfOneLinearArgument(expr, x, integrateByParts)) is { }) return answer;
            // Two tangents, cotangents, secants or cosecants of arguments a constant apart, written
            // as functions of each alone by the addition formulas.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingTwoFunctionsOfShiftedArgumentsApart(expr, x, integrateByParts)) is { }) return answer;
            // A sine or a cosine of one argument beside functions of another shifted from it: the addition
            // formula writes it in the other.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingASineOfAShiftedArgumentInTheOther(expr, x, integrateByParts)) is { }) return answer;
            // A constant out of a fractional power of a trigonometric factor: `sqrt(b sec(x))` is
            // `sqrt(b) sqrt(sec(x))` for a positive `b`, which meets the other powers of the
            // secant beside it. After the rules that answer the same shapes for any real
            // constant -- a function out of its even power with its sign, the half angle of
            // `a +- a sin(y)` -- since this one owes `provided b > 0` and, asked first, answered
            // `sqrt(a + a sin(x))` for a positive `a` only. Before the substitution search, which
            // spends the budget on the roots.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingAConstantOutOfAFractionalPower(expr, x, integrateByParts)) is { }) return answer;
            // Two roots of linears with one slope, rationalised together by their sum: before the
            // substitution search, which reads each root on its own.
            if ((answer = IndefiniteIntegralSolver.SolveBySubstitutingTheSumOfTwoRootsOfLinears(expr, x, integrateByParts)) is { }) return answer;
            // A function of `x^n` for a symbolic `n` beside a power of `x`, under `u = x^(n/d)`:
            // before the substitution search, which reads `x^(2n)` as a power of `x^n` only for a
            // number `n`.
            if ((answer = IndefiniteIntegralSolver.SolveByAPowerOfXWithASymbolicExponent(expr, x, integrateByParts)) is { }) return answer;
            // A power of x times the inverse hyperbolic secant or cosecant of `sqrt(x)`, by parts onto
            // `x^m/sqrt(1 -+ x)`: before the substitution search, whose `u = sqrt(x)` asked the
            // question in u one level down, where it ran out of time.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstAnInverseHyperbolicSecantOfARoot(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveBySubstitution(expr, x, integrateByParts)) is { }) return answer;
            // A constant times a whole power of something of x under a power that is not whole, the
            // whole power taken out with the factor constant where it is not zero:
            // `x^2 (c (a + b x^2)^2)^(3/2)` is that factor times a polynomial. After the substitution
            // search, which answers a power of a linear and an odd power of x beside a function of
            // x^2 more shortly: `1/(c (a + b x)^2)^(5/2)` in 64 characters, where this writes 98.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingAWholePowerOutOfAPowerOfAProduct(expr, x, integrateByParts)) is { }) return answer;
            // A logarithmic derivative the substitution above could not read for a symbol in
            // an exponent: `(x^(n-1) - 1)/(x^n - n x)`.
            if ((answer = IndefiniteIntegralSolver.SolveALogarithmicDerivativeWithASymbolicExponent(expr, x)) is { }) return answer;
            // And a power of a sum beside its derivative, with functions of x besides powers of it in the terms.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfASumBesideItsDerivativeWithSymbolicPowers(expr, x)) is { }) return answer;
            // After the general substitution rather than inside it, because the general one
            // divides by du/dx and asks what is left, and that question loses the shape here:
            // sqrt(tan(x)) over the derivative of sqrt(tan(x)) simplifies to sin(2x), in which
            // the substitution is no longer visible. This one rewrites rather than divides.
            // Before the tangent substitution, which would also take a power of the secant and
            // answer it as a rational function of tan(x) -- correct, and a good deal longer than
            // the reduction gives. Rubi's own ordering puts the reduction first for the same
            // reason.
            // A polynomial times an exponential times a sine or a cosine, which repeated by parts
            // closes in as many rounds as the polynomial has degree. Before the trigonometric
            // rules, because none of them reads the exponential and all of them would have to
            // decline it.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialTimesAnExponentialAndATrigonometric(expr, x)) is { }) return answer;
            // And the same shape with a phase in the trigonometric's argument, expanded by the
            // angle-sum identity so that the rule above reads one frequency and no phase.
            if ((answer = IndefiniteIntegralSolver.SolveByExpandingATrigonometricPhaseBesideAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // A half-integer power of `1 + sin` or `1 - cos` and their kin, closed by one
            // cancellation that `a^2 = b^2` allows. Before the reductions, which do not read a
            // fractional power at all.
            if ((answer = IndefiniteIntegralSolver.SolveAHalfPowerOfOnePlusASine(expr, x)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveBySecantPowerReduction(expr, x)) is { }) return answer;
            // And beside it: a power of the sine times a power of the cosine, which is every
            // product of the six trigonometric functions once tangents and secants are read as
            // the pair of exponents they are. After the reduction, because a power of the secant
            // alone is both rules' and the reduction's answer for it is shorter.
            if ((answer = IndefiniteIntegralSolver.SolveByTrigonometricPowerSubstitution(expr, x)) is { }) return answer;
            // A power of the cotangent that is not whole beside the tangent, written as the
            // tangent's with the constant that takes in front, which the substitution below reads.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfTheCotangentInTheTangent(expr, x, integrateByParts)) is { }) return answer;
            // A difference of a function and its reciprocal's partner, `csc(z) - sin(z)`, as the
            // quotient of powers it is, for the rule after it.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingADifferenceOfReciprocalFunctionsAsAQuotient(expr, x, integrateByParts)) is { }) return answer;
            // A product of powers of the trigonometric functions of one argument, some not whole,
            // whose powers of the sine and cosine add up to an even number: the tangent's form
            // times a constant, which the substitution below reads.
            if ((answer = IndefiniteIntegralSolver.SolveAProductOfPowersOfTrigonometricFunctionsThroughTheTangent(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByTangentSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // A root of a quadratic in the tangent with a linear term, rotated until it has
            // none: `1/sqrt(a + b tan(x) + c tan(x)^2)` is `1/sqrt(A + C tan(y)^2)` under
            // `x = y + arctan(m)`, which the substitution above then answers.
            if ((answer = IndefiniteIntegralSolver.SolveByRotatingAwayTheLinearTermInTheTangent(expr, x, integrateByParts)) is { }) return answer;
            // And the logarithm's own substitution, beside the tangent's. It goes the other way
            // -- `x = e^u`, so it *introduces* an exponential rather than cancelling one -- which
            // is why the general substitution does not find it and why it pays: the integrator
            // answers an exponential times almost anything.
            if ((answer = IndefiniteIntegralSolver.SolveByLogarithmSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // And a power of x times a function of one logarithm of a monomial, under t = that logarithm.
            if ((answer = IndefiniteIntegralSolver.SolveByAPowerAndALogarithmOfAMonomial(expr, x, integrateByParts)) is { }) return answer;
            // And the inverse trigonometric functions' own, beside the logarithm's and for the
            // same reason: `x = sin(u)` removes the `x` that substituting for `arcsin(x)` leaves.
            if ((answer = IndefiniteIntegralSolver.SolveByInverseTrigonometricSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // And the inverse hyperbolic functions', read off the logarithm each is written as:
            // `x = sinh(u)` for `ln(x + sqrt(x^2 + 1))`.
            if ((answer = IndefiniteIntegralSolver.SolveByInverseHyperbolicSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of i times an inverse trigonometric function is algebraic:
            // `e^(i arctan(a x))` is `(1 + i a x)/sqrt(1 + a^2 x^2)`.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnExponentialOfAnInverseAlgebraically(expr, x, integrateByParts)) is { }) return answer;
            // And the third of Bioche's rules: a quotient of homogeneous polynomials in sine and
            // cosine, which the tangent turns into a rational function whenever the two degrees
            // differ by an even number. After the tangent substitution above, which answers an
            // integrand that is a function of the tangent *alone* and gives a shorter answer for
            // it; this one is for the rest.
            if ((answer = IndefiniteIntegralSolver.SolveByHomogeneousTrigonometricSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // And the odd case it leaves: `a cos(y) + b sin(y)` is one cosine of a shifted
            // argument, `R cos(u)`, and the quotient over a power of it a quotient over a power of
            // that cosine, which the rules for powers of the sine and cosine answer.
            if ((answer = IndefiniteIntegralSolver.SolveByRotatingACosineAndASineIntoOneCosine(expr, x, integrateByParts)) is { }) return answer;
            // An even polynomial over a biquadratic with symbols in it, over the two roots
            // in x^2 written with the root of the discriminant: partial fractions read a
            // written factor, and `a + b x^2 + c x^4` is written as one.
            if ((answer = IndefiniteIntegralSolver.SolveAnEvenPolynomialOverASymbolicBiquadratic(expr, x, integrateByParts)) is { }) return answer;
            // And over a power of a linear in x^2 beside the biquadratic, split in x^2 minus its root.
            if ((answer = IndefiniteIntegralSolver.SolveAnEvenQuotientOverAPowerOfALinearInTheSquareAndABiquadratic(expr, x, integrateByParts)) is { }) return answer;
            // And over a power of a biquadratic beside another factor in x^2, split in x^2 itself.
            if ((answer = IndefiniteIntegralSolver.SolveAnEvenQuotientOverAPowerOfABiquadraticBesideAnotherFactorInTheSquare(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByPartialFractions(expr, x, integrateByParts)) is { }) return answer;
            // A whole negative power of a polynomial of several terms among the factors,
            // written below the bar and asked again: the gathering on the way in writes
            // `u^3/((a u^2 + b)^3 u)` as `u^2 (a u^2 + b)^(-3)`, a product with a power in it
            // that the symbolic quadratic rule does not read, where `u^2/(a u^2 + b)^3` is
            // answered at once. After the partial fractions, which read the product spelling
            // and cancel first: Timofeev's `sin(x)^6 tan(x)/cos(2x)^(3/4)` under its root is
            // a product whose bracket cancels the power, a line there and a page as a
            // quotient over the quartic asked from the top; and after the substitution
            // search, whose `u/(a u^2 + b)^2` is `-1/(2a (a u^2 + b))` where the symbolic
            // quadratic answers with a piecewise on the discriminant.
            if ((answer = IndefiniteIntegralSolver.SolveWithPolynomialPowersBelowTheBar(expr, x, integrateByParts)) is { }) return answer;
            // What the rational splits could not take apart: a denominator irreducible over
            // the rationals past degree two, or one whose real factors carry the roots of a
            // cubic. The residues decide the field the answer needs, not the roots of the
            // denominator, and the Rothstein-Trager resultant finds them.
            if ((answer = IndefiniteIntegralSolver.SolveByRothsteinTrager(expr, x)) is { }) return answer;
            // A rational function with complex coefficients, written as its real and imaginary parts
            // over a real denominator, which the rules above read.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionWithComplexCoefficients(expr, x, integrateByParts)) is { }) return answer;
            // The tangent substitution again, and this time for a *rational* function: a repeated
            // irreducible quadratic beside a negative power of the variable is `sin^p cos^q` with
            // both exponents whole, which the recurrences close. Below partial fractions rather
            // than beside it, so that everything the rational split answers keeps the form it
            // gives -- this only ever sees what that declined.
            if ((answer = IndefiniteIntegralSolver.SolveARadicalOfAQuadraticAsTrigonometric(
                    expr, x, aWholePowerCounts: true)) is { }) return answer;
            // Linearity again, and this time with the expansion: a product with a sum in it --
            // sin(a+f*x)^4 * (5 - 6*sin(a+f*x)^2) -- reaches this as a product, so only the
            // expansion finds the two terms it is. Behind by parts it never did: the search spent
            // the whole budget first and the caller waited over 20 seconds to be told nothing,
            // where the split answers in 0.16.
            // https://github.com/asc-community/AngouriMath/issues/779
            //
            // It cannot cost an answer: splitting returns null unless *every* term integrates, so
            // a sum that only comes out whole still falls through to by parts below. Expansion is
            // bounded by MaxExpansionTermCount, which returns null rather than building the terms.
            // A root of exponentials of both signs is under `u = e^x` a root of a quartic that
            // nothing rationalises, and under the hyperbolic tangent the hyperbolic functions
            // are the shapes their trigonometric twins are under the tangent. Before the
            // expanding split, which wrote `tanh(x)^5` as six terms and sent each through the
            // substitution on its own; and before the exponential substitution, which
            // answered Timofeev's hyperbolic 560 in a minute through Euler's substitution on
            // that quartic.
            if ((answer = IndefiniteIntegralSolver.SolveByHyperbolicTangentSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // Hyperbolic powers two apart whose coefficients kill the reduction's residual:
            // `cosh(y)^p - (p - 1)/p cosh(y)^(p - 2)` is `sinh(y) cosh(y)^(p - 1)/p`, where
            // neither power alone is elementary. Before the split, which would hand each
            // elliptic term to the whole chain and spend a minute finding that out.
            if ((answer = IndefiniteIntegralSolver.SolveAPairOfHyperbolicPowersTwoApart(expr, x, integrateByParts)) is { })
                return answer;
            // A power of `d + e x + f sqrt(Q)` with `e^2 = a f^2`, `a` the leading coefficient of
            // `Q`, is a power of Euler's variable: before the expanding split, which wrote
            // `Q^2 (d + e x + f sqrt(Q))^n` as five terms and answered each on its own, at ten
            // times the length of the one answer.
            if (IndefiniteIntegralSolver.HoldsAPowerOfASumWithARoot(expr, x)
                && (answer = IndefiniteIntegralSolver.SolveByEulerSubstitution(expr, x, onlyBySumItWrites: true)) is { }) return answer;
            // A polynomial beside two powers of linears whose exponents add up to -1, reduced to the two
            // powers alone, before the split writes the polynomial out: each term of
            // `(e + f x)^3/((a + b x)^(1/3) (c + d x)^(2/3))` took seconds by the quotient of the roots.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialBesideTwoLinearPowersWhoseExponentsAddUpToMinusOne(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveBySplittingSum(expr, x, integrateByParts)) is { }) return answer;
            // A sum whose terms all carry the same power of one linear form, as that power times
            // the sum of the rest: `x cosh(x)^(3/2) - x sqrt(cosh(x))/3` is `x` times a pair
            // neither half of which is elementary, which the split cannot answer and parts can.
            if (expr is Entity.Sumf or Entity.Minusf
                && (answer = IndefiniteIntegralSolver.SolveByGatheringACommonPolynomialFactorOfASum(expr, x, integrateByParts)) is { })
                return answer;
            // A cubic below the bar with symbolic coefficients and a repeated root, written over
            // its roots: `1/(x^3 - 3b x + 2b^(3/2))` is `1/((x - sqrt(b))^2 (x + 2 sqrt(b)))`.
            if (expr is Entity.Divf or Entity.Powf
                && (answer = IndefiniteIntegralSolver.SolveACubicWithARepeatedRootOverItsRoots(expr, x, integrateByParts)) is { })
                return answer;
            // The same factor taken out of a sum below the bar: `1/(a x + b x/ln(c x^n))` is
            // `1/(x (a + b/ln(c x^n)))`, a function of the logarithm beside its differential.
            if (expr is Entity.Divf or Entity.Powf
                && (answer = IndefiniteIntegralSolver.SolveByGatheringACommonLinearFactorOfASumBelowTheBar(expr, x, integrateByParts)) is { })
                return answer;
            // A power of the secant that is not whole beside the cosine, written as the cosine's
            // with the constant that takes in front, which the substitutions below read.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfTheSecantInTheCosine(expr, x, integrateByParts)) is { }) return answer;
            // The half-angle substitution goes *after* linearity, and that is not a preference.
            // It fires on anything built from sines and cosines, and it answers `cos(x) + 1` with
            // a correct expression in tan(x/2) some forty characters long where splitting the sum
            // answers `sin(x) + x`. Both are antiderivatives; only one is an answer anybody wants,
            // and the F# wrapper's test pinned the readable one. So everything that decomposes the
            // problem into pieces the ordinary rules know gets first refusal, and this sees only
            // what is left — which is the quotients it was added for, since linearity declines a
            // quotient and partial fractions declines one that is not a ratio of polynomials.
            // Bioche's first two rules before the third: a rational function of the two that
            // is odd in one of them is a rational function of the other, smaller than the
            // half-angle tangent's and answered more shortly.
            // A rational function of the secant alone in the cosine first, as one quotient: the
            // half-angle tangent of the secant's spelling was a search past thirty seconds.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingARationalFunctionOfTheSecantInTheCosine(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByBiochesOddSubstitution(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByHalfAngleSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // The substitution by a sine or a cosine where an odd power of the complement is
            // left: its sign times a root, the sign a constant between the complement's zeros.
            // After the half-angle substitution, which answers the rational functions of the
            // two more shortly.
            if ((answer = IndefiniteIntegralSolver.SolveByTheSignOfTheComplement(expr, x, integrateByParts)) is { }) return answer;
            // Beside the half-angle one and for the same reason: it rewrites the integrand into a
            // rational function, so it wants everything that answers a problem in its own terms to
            // have declined first.
            if ((answer = IndefiniteIntegralSolver.SolveByLinearRadicalSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // Two linear bases under roots of different orders, which the rule above declines:
            // the root of their quotient rationalises them where the powers left are whole.
            if ((answer = IndefiniteIntegralSolver.SolveByAQuotientOfTwoLinearRadicals(expr, x, integrateByParts)) is { }) return answer;
            // A square root of a quadratic, answered as a power of the sine times a power of the
            // cosine rather than by rationalising it. After the linear radical, which answers a
            // root of something linear in its own terms and more shortly.
            if ((answer = IndefiniteIntegralSolver.SolveARadicalOfAQuadraticAsTrigonometric(expr, x)) is { }) return answer;
            // And the binomial differential, a power of the variable beside a fractional power of
            // `a + b x^n`. After the quadratic, which is the case `n = 2` and answers it through
            // the trigonometric substitution -- a shorter answer than a root of a root.
            if ((answer = IndefiniteIntegralSolver.SolveABinomialDifferential(expr, x)) is { }) return answer;
            // And a root of a quadratic binomial beside another at the ratios where the integral is
            // elementary: `1/((1 - x^2)^(1/3) (3 + x^2))`, which is no binomial differential.
            if ((answer = IndefiniteIntegralSolver.SolveAnEllipticLookingQuotientOfBinomials(expr, x)) is { }) return answer;
            // A rational function of x^n beside `(c + d x^n)^(k - 1/n)`, rationalised by
            // `u = x/(c + d x^n)^(1/n)`: Timofeev's `1/((1 + x^4)(2 + x^4)^(1/4))` is `1/(1 + u^4)`.
            if ((answer = IndefiniteIntegralSolver.SolveByDividingByTheRoot(expr, x)) is { }) return answer;
            // And a rational function of x and one square root of a quadratic, rationalised by
            // Euler's substitution and finished by the rational integrator directly. After the
            // trigonometric substitution and the binomial differential, which answer their own
            // shapes more shortly; this is for everything else with one such root in it. Late
            // and not scoped, and both were measured: placed before the general substitution it
            // answered that rule's sub-integrals and let a doomed search run for thirty seconds
            // (the #1265 failure), and scoped it declined nine integrands that reach it one
            // level down -- the nested radicals under `u = sqrt(1 + x)`, the remainders of by
            // parts -- each of which is closed here.
            // A polynomial times an odd half power of a quadratic, reduced to
            // `R Q^(k + 1/2) + K/sqrt(Q)` by one solve, and the last term the table's -- a
            // piecewise on the sign of a symbolic leading coefficient, where the
            // substitutions in front have no sign to go on.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialTimesAnOddHalfPowerOfAQuadratic(expr, x)) is { }) return answer;
            // And with a power of x below, two remainders: `sqrt(a^2 - x^2)/x^2` is
            // `-sqrt(a^2 - x^2)/x - arcsin(x/a)`.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverAPowerOfXTimesAnOddHalfPowerOfAQuadratic(expr, x)) is { }) return answer;
            // And the same with an exponential of a linear in front, an ansatz with no
            // remainder: `e^x (1 - x - x^2)/sqrt(1 - x^2)` is `(e^x sqrt(1 - x^2))'`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialTimesAnOddHalfPowerOfAQuadratic(expr, x)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveALinearBesideTheRootOfAQuadratic(expr, x)) is { }) return answer;
            // And over a quadratic, closed as well: beside the linear's, since each of the two is
            // a piece the rational function below is taken apart into.
            if ((answer = IndefiniteIntegralSolver.SolveALinearOverAQuadraticBesideTheRootOfAnother(expr, x)) is { }) return answer;
            // And over a power of the quadratic, a power at a time down to that one.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialOverAPowerOfAQuadraticBesideTheRootOfAnother(expr, x)) is { }) return answer;
            // A rational function of a quadratic beside a half-odd power of it, under t = Q'/sqrt(Q):
            // after the reduction over a power of the quadratic, which answers the third and fourth
            // powers of `1/((d + b x + c x^2)^k sqrt(a + b x + c x^2))` in a third of the characters.
            if ((answer = IndefiniteIntegralSolver.SolveAFunctionOfAQuadraticBesideItsRootUnderItsDerivativeOverTheRoot(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionBesideTheRootOfAQuadratic(expr, x)) is { }) return answer;
            // A linear below the bar that divides the radicand, written over it: after the two
            // rules above, which read a linear beside the root as a pole and find nothing to take
            // apart where its residue, the radicand at its root, is zero.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingALinearBelowTheBarOverTheRadicand(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByEulerSubstitution(expr, x)) is { }) return answer;
            // And a half-odd power of L + sqrt(Q) where Q - L^2 is a square, in u = (sqrt(Q) - L)/(x - r),
            // of which x, the inner root and the outer one's square are rational functions:
            // Bondarenko's `sqrt(1 + sqrt(x) + sqrt(1 + 2x + 2 sqrt(x)))` under x = t^2.
            if ((answer = IndefiniteIntegralSolver.SolveAHalfOddPowerOfALinearPlusTheRootOfAQuadraticByEulersSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // And a root over the root of a quadratic, or over the roots of two linears with one
            // slope, in the variable that rationalises the roots inside: `sqrt(1 - x^2 + x sqrt(x^2 - 1))`.
            if ((answer = IndefiniteIntegralSolver.SolveANestedRootByRationalisingTheRootsInIt(expr, x, integrateByParts)) is { }) return answer;
            // And a root of a palindromic quartic, which is a root of a quadratic in x -+ 1/x:
            // Charlwood's `(1 + x^2)/((1 - x^2) sqrt(1 + x^4))` is `-du/(u sqrt(u^2 + 2))`.
            if ((answer = IndefiniteIntegralSolver.SolveByReciprocalSubstitution(expr, x)) is { }) return answer;
            // And a power of the variable below the bar beside a root of a quadratic, by the
            // same reciprocal: `1/(x^2 sqrt(Q))` is a polynomial over the root of the reversed
            // quadratic, which the rules for those answer.
            if ((answer = IndefiniteIntegralSolver.SolveByTheReciprocalBesideARootOfAQuadratic(expr, x, integrateByParts)) is { }) return answer;
            // The binomial differential with a symbol in its coefficients, asked here rather than
            // beside the one with numbers: the rules since then answer what they share with it
            // more shortly, `1/(a - b x^4)^(1/4)` by two arctangents where this gives four terms.
            if ((answer = IndefiniteIntegralSolver.SolveABinomialDifferentialWithSymbols(expr, x)) is { }) return answer;
            // An algebraic function whose symbols a scaling of x takes out, as a constant times one with
            // numbers: `1/((c + d x) (2 c^3 + d^3 x^3)^(1/3))` in y = d x/c, which the ansatz below answers;
            // and before the ansatz, which ran past the budget on `1/((1 + 2^(1/3) x) (1 + x^3)^(2/3))`.
            if ((answer = IndefiniteIntegralSolver.SolveByScalingTheVariableToNumbers(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of x and one cube root of a polynomial: no substitution
            // rationalises it, and the elementary ones are logarithms of `L - y` for linear L
            // whose cube agrees with the polynomial at the poles, found by an ansatz.
            if ((answer = CubeRootLogarithmAnsatz.Integrate(expr, x)) is { }) return answer;
            // And a quotient beside the cube root of a polynomial with a linear factor L, at the
            // coefficients where it is a constant times T'/(1 - T^3) for T = m L/y or m y/L, in
            // closed form: after the ansatz, which answers the numeric ones and declined Welz's,
            // with symbols in the radicand.
            if ((answer = IndefiniteIntegralSolver.SolveAPseudoEllipticQuotientBesideACubeRootWithALinearFactor(expr, x)) is { }) return answer;
            // And of one square root of a squarefree quadratic, cubic or quartic -- the latter
            // two elliptic, the first rational but with residues Euler's substitutions leave in
            // a field of degree four: the elementary ones are logarithms and arctangents of
            // `A - B y` whose A/B agrees with a branch of y at the poles, found by an ansatz
            // over Padé approximants.
            if ((answer = SquareRootLogarithmAnsatz.Integrate(expr, x)) is { }) return answer;
            // `(x + s)/((x^2 + s x + s^2) (a + b x^3)^(1/3))`, `(s^2 - x^2)/((s^3 - x^3) (a + b x^3)^(1/3))`
            // in two elementary terms, in closed form: after the ansatz over a cube root, which
            // answers most of the numeric spellings and declined Welz's, with symbols in the binomial.
            if ((answer = IndefiniteIntegralSolver.SolveALinearOverTheQuadraticFactorOfADifferenceOfCubesBesideACubeRoot(expr, x)) is { }) return answer;
            // And a sum with the root below the bar, over its conjugate: after every rule above,
            // which answer the numeric spellings, `1/(a c + b c x^2 + d sqrt(a + b x^2))` among them.
            if ((answer = IndefiniteIntegralSolver.SolveByTheConjugateOfASumWithARootOfAQuadraticBelowTheBar(expr, x, integrateByParts)) is { }) return answer;
            // A linear over a linear beside the root of a cubic or a quartic, where the integral is a
            // function of the numerator's square over the root: Welz's
            // `(1 + x - sqrt(3))/((1 + x + sqrt(3)) sqrt(x^4 + 4 sqrt(3) x^2 - 4))`, whose pole is
            // irrational. After the ansatz, which answers the ones with rational poles.
            if ((answer = IndefiniteIntegralSolver.SolveALinearOverALinearBesideTheRootOfAQuarticByTheSquareOfTheNumerator(expr, x)) is { }) return answer;
            // And a power of a product with a factor under a whole power, that power taken out in
            // front of a constant: `(c sin(a + b x)^3)^(1/3)` is `K sin(a + b x)`.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingTheWholePowersOutOfAPowerOfAProduct(expr, x, integrateByParts)) is { }) return answer;
            // Product-to-sum among the rewrites rather than before them, because a product of
            // trigonometric functions of *equal* arguments is a power and wants a different tool;
            // this only fires where the arguments differ, which is exactly what every substitution
            // above declines for want of an inner function to substitute for.
            if ((answer = IndefiniteIntegralSolver.SolveByProductToSum(expr, x, integrateByParts)) is { }) return answer;
            // And the other way round: not a product of two arguments but anything else built
            // from different multiples of one -- `sin(x)/cos(2x)`, `cos(x)/(sin(x) tan(x/2))` --
            // rewritten to the one argument every trigonometric rule above reads.
            // A sine of the double argument beside the argument: one product, for any slope.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingASineOfADoubleArgumentAsAProduct(expr, x, integrateByParts)) is { }) return answer;
            if ((answer = IndefiniteIntegralSolver.SolveByUnifyingTrigonometricArguments(expr, x, integrateByParts)) is { }) return answer;
            // The exponential substitution beside the other rewrites. It is also what integrates
            // the hyperbolic functions, which are not nodes here but quotients of exponentials.
            if ((answer = IndefiniteIntegralSolver.SolveByExponentialSubstitution(expr, x, integrateByParts)) is { }) return answer;
            // And a rational one, where the exponential substitution has declined it.
            if ((answer = IndefiniteIntegralSolver.SolveAHyperbolicIntegrandAsATrigonometricOne(expr, x, integrateByParts, rationalToo: true)) is { }) return answer;
            // A quotient by an exponential is the product with its reciprocal, and only that
            // spelling reaches integration by parts. After the substitution above rather than
            // before it, since a quotient that is rational in e^(k x) is that rule's to answer
            // whole and comes out in better shape for it.
            if ((answer = IndefiniteIntegralSolver.SolveByDividingByAnExponential(expr, x, integrateByParts)) is { }) return answer;
            // An exponential of something rational times something rational, as the derivative
            // of `e^h N/D`: Liouville says that is the only elementary shape it can have, and
            // an ansatz finds it or nothing does. After the substitutions, which answer the
            // linear-exponent cases in their own terms.
            if ((answer = IndefiniteIntegralSolver.SolveByExponentialAnsatz(expr, x)) is { }) return answer;
            // And where it declines for a simple pole at a zero of the exponent, the derivative of a
            // multiple of Ei(h) taken out first: Hebisch's
            // `(2 + 2x + 3x^2 - x^3 + 2x^4) e^(x/(2 + x^2))/(2x + x^3)`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialBesideAMultipleOfTheDerivativeOfItsExponentialIntegral(expr, x)) is { }) return answer;
            // An exponential of a polynomial in the reciprocal of a linear that is not a Gaussian,
            // under the Gaussian's substitution `u = 1/L`. After the ansatz, which answers some of
            // these in the base: `f^(a + b/x)/x^4` is `f^(a + b/x)` times a polynomial in `1/x`.
            if ((answer = IndefiniteIntegralSolver.SolveAnExponentialInTheReciprocalOfALinear(expr, x, integrateByParts)) is { }) return answer;
            // A polynomial times a fractional power of a base that holds a function of x,
            // answered as a polynomial times the next power of the base: a linear system in
            // the polynomial's coefficients, exact, and volunteered at any depth.
            if ((answer = IndefiniteIntegralSolver.SolveByAPolynomialTimesAPowerOfTheBase(expr, x)) is { }) return answer;
            // And a polynomial times powers of polynomials, one at least not whole, as another
            // polynomial times each power raised by one: Rubi's
            // `(a g + e x - b g x^4)/(a + b x^4)^(3/2)` and `(d + f x^2)^p (...)`, which no rule for
            // a root of a quadratic reads. After the substitutions, which answer their shapes first.
            if ((answer = IndefiniteIntegralSolver.SolveAPolynomialTimesPowersOfPolynomialsByRaisingThePowers(expr, x)) is { }) return answer;
            // And an exponential times a rational function of the sine and cosine, closed by
            // the same kind of ansatz in the half-angle tangent: `e^x (1 - sin(x))/(1 - cos(x))`
            // is `-e^x cot(x/2)`.
            if ((answer = IndefiniteIntegralSolver.SolveByExponentialHalfAngleAnsatz(expr, x)) is { }) return answer;
            // And one level up the tower: an exponential -- of something that may hold ln(x)
            // -- times a rational function of x and ln(x). `(-1 + (1 - x) ln(x))/(e^x ln(x)^2)`
            // is `(x e^(-x)/ln(x))'`.
            if ((answer = IndefiniteIntegralSolver.SolveByLogarithmTowerAnsatz(expr, x)) is { }) return answer;
            // And the same tower with the sine and cosine for the logarithm: a rational
            // function of x and of sin(x) and cos(x) together. `x^2/(x cos(x) - sin(x))^2` is
            // `((x sin(x) + cos(x))/(x cos(x) - sin(x)))'`.
            if ((answer = IndefiniteIntegralSolver.SolveByTrigonometricTowerAnsatz(expr, x)) is { }) return answer;
            // Powers of polynomials that are multiples of powers of one linear, as one power of it,
            // the quotient constant in front. Late, so that a rule answering the powers as written
            // goes first: asked before the rules for two linears, it wrote `((a + b x)^3)^(1/3)` in
            // 111 characters where they write 25.
            if ((answer = IndefiniteIntegralSolver.SolvePowersOfOneLinearAsOnePower(expr, x, integrateByParts)) is { }) return answer;
            // And beside a polynomial too, where everything before has declined: `x ln(e (f (a + b x)^p (c + d x)^q)^r)`.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionTimesALogarithmOfPowersOfLinears(expr, x, integrateByParts, besideAPolynomialToo: true)) is { }) return answer;
            // A product of powers of a sine and a cosine beside something that is no polynomial,
            // linearised, where everything answering it as written has declined: `sin(u)^3 cos(u)^6/(a + b u)`.
            if ((answer = IndefiniteIntegralSolver.SolveAProductOfPowersOfASineAndACosineByLinearising(expr, x)) is { }) return answer;
            // Last of the rewrites, because it is the only one that fires on an integrand nothing
            // is wrong with -- it clears a parameter rather than a shape -- so everything that
            // can answer the problem as written gets to try first.
            if ((answer = IndefiniteIntegralSolver.SolveByScalingTheVariable(expr, x, integrateByParts)) is { }) return answer;
            // And a linear over the root of a quartic whose symbols are a scale of x, where that
            // takes them out from under the root, which the simplifier does not: the logarithmic
            // ansatz answers the numeric quartic that is left.
            if ((answer = IndefiniteIntegralSolver.SolveALinearOverTheRootOfAQuarticWithItsSymbolsScaledOut(expr, x, integrateByParts)) is { }) return answer;
            // After every rule that can answer a power in its own terms, because expanding one
            // throws away whatever structure it had: (1 + x^2)^2 is answered as a power and only
            // wants writing out if that fails.
            if ((answer = IndefiniteIntegralSolver.SolveByExpandingAPower(expr, x, integrateByParts)) is { }) return answer;
            // And a product of such powers and sums, written out for the same reason and as late.
            if ((answer = IndefiniteIntegralSolver.SolveByExpandingAProductOfSums(expr, x, integrateByParts)) is { }) return answer;
            // One block quadratic in a power of x, split at its roots, which are complex where its
            // discriminant is negative: as late, so that a rule answering the block whole goes first.
            if ((answer = IndefiniteIntegralSolver.SolveByOneBlockInAPowerOfXAtItsRoots(expr, x, integrateByParts)) is { }) return answer;
            // A rational function with symbols in it beside a half-odd power of a quadratic, its polynomial
            // part divided out and the rest split over the factors below the bar first.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionBesideAHalfOddPowerOfAQuadraticSplitFirst(expr, x, integrateByParts)) is { }) return answer;
            // A power of x that is not whole beside a polynomial in x^n over a whole power below -1 of a
            // binomial in x^n, each term reduced to the first power: `1/(x^(3/2) (a + c x^4)^2)`.
            if ((answer = IndefiniteIntegralSolver.SolveAFractionalPowerOfXBesideABinomialToAWholePowerBelowMinusOne(expr, x, integrateByParts)) is { }) return answer;
            // And beside a trinomial in x^n: `1/(x^(3/2) (a + b x^2 + c x^4)^2)`.
            if ((answer = IndefiniteIntegralSolver.SolveAFractionalPowerOfXBesideATrinomialToAWholePowerBelowMinusOne(expr, x, integrateByParts)) is { }) return answer;
            // A sum whose terms share a power that is not whole of one polynomial, as that power times
            // the rest: `(1 - x^6)^(2/3) + (1 - x^6)^(2/3)/x^6`, whose terms apart are not elementary.
            if ((answer = IndefiniteIntegralSolver.SolveASumOfTermsSharingAPowerOfAPolynomialAsOneProduct(expr, x, integrateByParts)) is { }) return answer;
            // A rational function over the root of a palindromic quartic, under u = x + 1/x, the root of
            // the quartic over x standing for the root of the quadratic in u.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOverTheRootOfAPalindromicQuarticUnderXPlusItsReciprocal(expr, x, integrateByParts)) is { }) return answer;
            // A root of a product with a root in one of its factors, written out under the root.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingOutAProductUnderANestedRoot(expr, x, integrateByParts)) is { }) return answer;
            // A rational function with symbols in it beside a root of a linear, split into partial
            // fractions first, each term the question of one factor beside the root.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionBesideARootOfALinearSplitFirst(expr, x, integrateByParts)) is { }) return answer;
            // A half-odd power of a constant over a polynomial of either sign, written apart with
            // its sign in front. After the substitutions, which answer some of these without it.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAHalfOddPowerOfAReciprocalWithItsSign(expr, x, integrateByParts)) is { }) return answer;
            // A power of x beside a whole power below -1 of a binomial with a symbol in it, by the
            // reduction that raises the power to -1: `1/(x^2 (a + b x^3)^5)`, which the partial
            // fractions declined past the first power.
            if ((answer = IndefiniteIntegralSolver.SolveABinomialToAWholePowerBelowMinusOneByTheRecurrence(expr, x, integrateByParts)) is { }) return answer;
            // A rational function beside a half-odd power of an even quartic P, as a rational
            // function of x/sqrt(P) where it is one: Rubi's pseudo-elliptic
            // `(a - c x^4)/((a d + a e x^2 + c d x^4) sqrt(a + b x^2 + c x^4))`. Late, since it fires
            // on nothing a rule reading the root as written answers.
            if ((answer = IndefiniteIntegralSolver.SolveByTheVariableOverTheRootOfAnEvenQuartic(expr, x, integrateByParts)) is { }) return answer;
            // A factor `(c x)^t` as `x^t`, `(c x)^t x^(-t)` in front, constant on each interval.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAPowerOfAMultipleOfXAsAPowerOfX(expr, x, integrateByParts)) is { }) return answer;
            // Two square roots as one, up to a factor whose square is a constant, in front:
            // `sqrt(b - a/x^2)/sqrt(a - b x^2)` is that factor over x.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingTwoRootsAsOneUpToAConstantFactor(expr, x, integrateByParts)) is { }) return answer;
            // Roots that are a constant times a power of the variable, though written with it, taken
            // out as that constant: `sin(x) sqrt(b - a/x^2)/sqrt(a - b x^2)` is K sin(x)/x. As late,
            // since it fires only where everything reading the roots as written has declined.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingOutRootsConstantUpToAPowerOfTheVariable(expr, x)) is { }) return answer;
            // The sign of a real-valued factor is constant between its zeros, and goes in
            // front of the antiderivative of the rest. After every rule that reads the sign
            // where it stands: `cos(x) sgn(sin(x))` is `|sin(x)|` by the substitution, and
            // `sgn(sin(x)) sin(x)` with the sign taken out first.
            if ((answer = IndefiniteIntegralSolver.SolveByTakingASignOut(expr, x, integrateByParts)) is { }) return answer;
            // An inverse hyperbolic function of `a x` over a half-odd power of an even quadratic, by
            // parts with the logarithm's derivative read off whole: the search's own parts split the
            // remainder into terms harder than it, and past `(c + d x^2)^(7/2)` ran out of time.
            if ((answer = IndefiniteIntegralSolver.SolveByPartsAgainstAnInverseHyperbolicOverAHalfOddPowerOfAQuadratic(expr, x, integrateByParts)) is { }) return answer;
            if (integrateByParts && (answer = IndefiniteIntegralSolver.SolveIntegratingByParts(expr, x)) is { }) return answer;
            // A hyperbolic integrand that is not rational in its exponential, asked as a trigonometric
            // one: last, since the question in s can take long where a rule above answers in x at once.
            if ((answer = IndefiniteIntegralSolver.SolveAHyperbolicIntegrandAsATrigonometricOne(expr, x, integrateByParts, rationalToo: false)) is { }) return answer;
            // A polynomial over a polynomial beside a power, divided, the part over the denominator
            // asked without the division's condition that the power is not zero: last, where nothing
            // has answered it with the condition, `x^2/((1 + x^2) (a + b x + c x^2)^(3/2))`.
            if ((answer = IndefiniteIntegralSolver.SolveByDividingWithoutTheConditionsOfTheDenominator(expr, x, integrateByParts)) is { }) return answer;
            // A rational function of x and ln(x) over distinct linears in the logarithm, by partial
            // fractions in it, where nothing has answered it whole: `(2 L^2 - L - x^2)/(L^3 - x^2 L)`.
            if ((answer = IndefiniteIntegralSolver.SolveARationalFunctionOfTheLogarithmByPartialFractionsInIt(expr, x, integrateByParts)) is { }) return answer;
            // A power of x below -1 beside a root of a binomial and a rational function of its
            // power, the pole at zero taken out by the reduction raising the power of x. Last:
            // the rules above answer the shapes they share with it in other spellings, and the
            // cube-root ansatz stops at the ninth degree of `1/(x^3 (1 - x^3)^(2/3) (1 + x^3))`.
            if ((answer = IndefiniteIntegralSolver.SolveANegativePowerOfXBesideARootOfABinomialByRaisingThePowerOfX(expr, x, integrateByParts)) is { }) return answer;
            // And a product of powers that is the derivative of one with symbolic exponents, its
            // constant read at a point where the quotient of the brackets does not simplify free of x.
            if ((answer = IndefiniteIntegralSolver.SolveAsTheDerivativeOfAProductOfPowersReadAtAPoint(expr, x, integrateByParts)) is { }) return answer;
            // A power of a logarithm beside a linear in another as two powers of the first, after the product of powers read
            // as a derivative: p x^m ln(x)^n ((m + 1) ln(x) + n + 1) is p x^(m + 1) ln(x)^(n + 1) there, and a sum of gamma
            // functions here. https://github.com/asc-community/AngouriMath/issues/2075
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfALogarithmBesideALinearInALogarithm(expr, x, integrateByParts)) is { }) return answer;
            // An arcoth of a linear as its artanh plus a constant, last of all, so that every arcoth
            // answered as written keeps that answer: `acoth(x)^2/(1 - x^2)^2` is
            // `(atanh(x) + K)^2/(1 - x^2)^2` with `K = acoth(x) - atanh(x)`, constant where it is
            // continuous, which the substitution under the hyperbolic tangent answers.
            if ((answer = IndefiniteIntegralSolver.SolveByWritingAnArcothAsAnArtanhPlusAConstant(expr, x, integrateByParts)) is { }) return answer;
            // And an inverse sine of a sine of a linear, which is linear on each interval its cosine keeps
            // a sign on. Last, so that every one answered as written keeps that answer: parts answers a
            // polynomial beside it.
            if ((answer = IndefiniteIntegralSolver.SolveByNamingAnInverseSineOfASineLinearOnEachInterval(expr, x, integrateByParts)) is { }) return answer;
            // And half-odd powers of a hyperbolic sine beside an exponential as the derivative of the same kind,
            // last: each term alone is not elementary, and the split declined them.
            if ((answer = IndefiniteIntegralSolver.SolveHalfOddPowersOfAHyperbolicSineBesideAnExponentialAsADerivative(expr, x)) is { }) return answer;
            // And a power that is not whole of a function of one exponential beside x, by an ansatz in that
            // power times a rational function of x and the exponential, last: by parts and the split leave
            // terms that are not elementary apart.
            if ((answer = IndefiniteIntegralSolver.SolveAPowerOfAFunctionOfAnExponentialBesideXByAnAnsatz(expr, x)) is { }) return answer;
            // Last, a gamma function at a whole or half-whole order, which the routes above answer as an elementary
            // function, an exponential integral or an error function where they can: by parts reaches
            // (c + d x)^(-1 - 3 n) F^(a + b (c + d x)^n) and not -1 - 4 n. https://github.com/asc-community/AngouriMath/issues/2075
            if ((answer = IntegralPatterns.APowerBesideAnExponentialOfAPowerAtAnyOrder(expr, x)) is { }) return answer;
            return null;
        }
        /// <summary>
        /// The integral of a function of one variable along the segment from <paramref name="from"/> to
        /// <paramref name="to"/>, by the five-point Gauss–Legendre rule on each of
        /// <paramref name="panels"/> equal panels, in double precision. See
        /// <see cref="Entity.DefiniteIntegral(Entity.Variable, EDecimal, EDecimal, int)"/>.
        /// </summary>
        /// <remarks>
        /// It was the mean of <c>panels + 1</c> equally weighted samples, the endpoints among them: a first-order
        /// rule, whose error on <c>x^2</c> over <c>[0, 1]</c> halved with each doubling, 1.67e-4 at 1000 steps,
        /// and printed to a hundred digits. And a convergent integral with a singular endpoint had no value:
        /// <c>sin(x)/x</c> and <c>ln(x)</c> over <c>[0, 1]</c> were NaN. Gauss–Legendre never evaluates an
        /// endpoint, and the five-point rule is exact on each panel for a polynomial of degree nine. It is run on
        /// the panels, twice as many and four times as many: where the differences do not shrink, the integral
        /// diverges at a bound and is NaN, and otherwise the rest of their geometric series is added and is the
        /// error, and the value is rounded to its place, fifteen digits at most. A singularity inside the
        /// segment is not seen, and a symmetric divergent one, as in 1/x over [-1, 1], comes to its principal
        /// value.
        /// https://github.com/asc-community/AngouriMath/issues/1675
        /// </remarks>
        /// <param name="expr">Expression to integrate</param>
        /// <param name="x">Variable to integrate over</param>
        /// <param name="from">The complex lower bound for integrating</param>
        /// <param name="to">The complex upper bound for integrating</param>
        /// <param name="panels">How many panels the segment is cut into, five evaluations each</param>
        internal static Complex IntegrateNumerically(Entity expr, Entity.Variable x, Complex from, Complex to, int panels = 100)
        {
            var function = expr.Compile(x);
            var start = from.ToNumerics();
            var length = to.ToNumerics() - start;
            panels = System.Math.Max(1, panels);
            // The integral on so many panels, and the integral of the modulus, which the rounding is measured
            // against: an integral that comes to zero, x^3 over [-1, 1], is a sum of terms that do not.
            (System.Numerics.Complex Value, double OfTheModulus) OnPanels(int count)
            {
                System.Numerics.Complex sum = 0;
                var ofTheModulus = 0.0;
                for (var i = 0; i < count; i++)
                {
                    var centre = start + length * ((i + 0.5) / count);
                    for (var k = 0; k < GaussLegendreNodes.Length; k++)
                    {
                        var term = GaussLegendreWeights[k] * function.Substitute(centre + length * (GaussLegendreNodes[k] / (2.0 * count)));
                        sum += term;
                        ofTheModulus += System.Numerics.Complex.Abs(term);
                    }
                }
                var width = System.Numerics.Complex.Abs(length) / (2.0 * count);
                return (sum * length / (2.0 * count), ofTheModulus * width);
            }
            // On the panels, twice as many and four times as many. Never evaluating a bound, the rule gives a
            // divergent integral a finite value: 1/x over [0, 1] came to about 10. Where it converges the
            // differences between the three shrink, by r = 2^-(1 - p) for an endpoint singularity like x^-p,
            // p < 1; for 1/x they stay at ln(2), and they grow for anything worse. So differences that do not
            // shrink, above the rounding, are NaN; and where they shrink, the rest of the geometric series,
            // d r/(1 - r) for the last difference d, is both the correction and the error: for x^-0.9 the last
            // difference is a fourteenth of the error, and the value without it was 7 where it is 10.
            var (coarse, _) = OnPanels(panels);
            var (middle, _) = OnPanels(2 * panels);
            var (fine, ofTheModulus) = OnPanels(4 * panels);
            if (double.IsNaN(fine.Real) || double.IsNaN(fine.Imaginary) || double.IsInfinity(fine.Real) || double.IsInfinity(fine.Imaginary))
                return fine.ToNumber();
            var rounding = 1e-14 * System.Math.Max(ofTheModulus, System.Numerics.Complex.Abs(fine));
            if (rounding == 0)
                return Integer.Zero;
            var first = System.Numerics.Complex.Abs(middle - coarse);
            var last = System.Numerics.Complex.Abs(fine - middle);
            if (last > rounding && last >= 0.99 * first)
                return Real.NaN;
            var value = fine;
            var error = System.Math.Max(last, rounding);
            if (last > rounding && first > rounding)
            {
                var ratio = last / first;
                var tail = (fine - middle) * (ratio / (1 - ratio));
                value = fine + tail;
                error = System.Math.Max(System.Numerics.Complex.Abs(tail), rounding);
            }
            // Then rounded to the place of that error, and to no more digits than a double holds: the value
            // was the double's exact binary expansion, fifty digits of which most were not known.
            var magnitude = System.Numerics.Complex.Abs(value);
            var quantum = (int)System.Math.Floor(System.Math.Log10(error));
            if (magnitude > 0)
                quantum = System.Math.Max(quantum, (int)System.Math.Floor(System.Math.Log10(magnitude)) - MostDigitsOfADouble + 1);
            // A part that rounds to zero is zero, which prints as 0 and not as -0E-15; and a value with no
            // imaginary part is real.
            EDecimal Rounded(double part)
                => EDecimal.FromDouble(part).Quantize(quantum, ERounding.HalfEven) is var rounded && rounded.IsZero ? EDecimal.Zero : rounded;
            var (realPart, imaginaryPart) = (Rounded(value.Real), Rounded(value.Imaginary));
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            return imaginaryPart.IsZero ? Real.Create(realPart) : Complex.Create(realPart, imaginaryPart);
        }

        private const int MostDigitsOfADouble = 15;

        // The five-point Gauss–Legendre rule on [-1, 1]: the roots of the fifth Legendre polynomial,
        // 0 and ±sqrt(5 ∓ 2 sqrt(10/7))/3, and their weights.
        [ConstantField]
        private static readonly double[] GaussLegendreNodes =
            { 0.0, -0.5384693101056831, 0.5384693101056831, -0.9061798459386640, 0.9061798459386640 };
        [ConstantField]
        private static readonly double[] GaussLegendreWeights =
            { 0.5688888888888889, 0.4786286704993665, 0.4786286704993665, 0.2369268850561891, 0.2369268850561891 };
    }
}
